using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SnakeController : MonoBehaviour
{
    // Settings
    public float MoveSpeed = 5;
    public float SteerSpeed = 180;
    public float BodySpeed = 5;
    public int Gap = 10;
    public int CorrectAnswersToLevelUp = 5;

    // References
    public GameObject BodyPrefab;
    public GameObject CorrectAnswer;
    public GameObject WrongAnswer;
    public GameObject DropTarget;
    public Text Question;
    public Text LevelText;
    public Text scoreText;
    public Text highScoreText;


    // Lists
    private List<GameObject> BodyParts = new List<GameObject>();
    private List<Vector3> PositionsHistory = new List<Vector3>();
    private Bounds dropTargetBounds;
    private int currentQuestionAnswer;
    private int currentLevel = 1;
    private int correctAnswersCount = 0;
    private int score;
    private int highScore;

    private List<GameObject> spawnedAnswers = new List<GameObject>();

    void Start()
    {
        if (PlayerPrefs.HasKey("HiScore"))
        {
            highScore = PlayerPrefs.GetInt("HiScore");
        }
        scoreText.text = "Score: " + score.ToString();
        highScoreText.text = "High Score " + highScore.ToString();
        // Initialize drop target bounds
        InitializeDropTargetBounds();
        // Create the first question and answers
        GenerateQuestionAndAnswers();
        UpdateLevelText();
    }
    void Update()
    {
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt("HiScore", highScore);
        }
        scoreText.text = "Score: " + score.ToString();
        highScoreText.text = "High Score " + highScore.ToString();
        // Move forward
        transform.position += transform.forward * MoveSpeed * Time.deltaTime;

        // Steer
        float steerDirection = Input.GetAxis("Horizontal"); // Returns value -1, 0, or 1
        transform.Rotate(Vector3.up * steerDirection * SteerSpeed * Time.deltaTime);

        // Store position history
        PositionsHistory.Insert(0, transform.position);

        // Move body parts
        int index = 0;
        foreach (var body in BodyParts)
        {
            Vector3 point = PositionsHistory[Mathf.Clamp(index * Gap, 0, PositionsHistory.Count - 1)];

            // Move body towards the point along the snake's path
            Vector3 moveDirection = point - body.transform.position;
            body.transform.position += moveDirection * BodySpeed * Time.deltaTime;

            // Rotate body towards the point along the snake's path
            body.transform.LookAt(point);

            index++;
        }

        // Keep the snake within the bounds
        KeepSnakeWithinBounds();
    }

    private void GrowSnake()
    {
        // Instantiate body instance and add it to the list
        GameObject body = Instantiate(BodyPrefab);
         // Ensure the tag is set correctly
        BodyParts.Add(body);
        score++;
        scoreText.text = "Score : " + score.ToString();
        StartCoroutine(ActivateCollision(body));
    }

    private IEnumerator ActivateCollision(GameObject body)
    {
        // Disable the collider initially
        body.GetComponent<Collider>().enabled = false;

        // Wait for a brief moment
        yield return new WaitForSeconds(0.5f);

        // Enable the collider after the delay
        body.GetComponent<Collider>().enabled = true;
    }

    private void CreateAnswer(GameObject answerPrefab, Vector3 position, string answerText)
    {
        GameObject answer = Instantiate(answerPrefab, position, Quaternion.identity);
        TextMesh textMesh = answer.GetComponent<TextMesh>();
        textMesh.text = answerText;
        spawnedAnswers.Add(answer);
    }

    private void GenerateQuestionAndAnswers()
    {
        // Generate a random number for the question based on the level's difficulty
        int maxNumber = 10 * currentLevel;
        currentQuestionAnswer = Random.Range(1, maxNumber);

        // Select a random operation
        string[] operations = { "+", "-", "*", "/" };
        string selectedOperation = operations[Random.Range(0, operations.Length)];

        int operand1 = Random.Range(1, maxNumber);
        int operand2 = selectedOperation == "/" ? Random.Range(1, maxNumber / operand1) : Random.Range(1, maxNumber);

        string correctAnswerText;
        switch (selectedOperation)
        {
            case "+":
                correctAnswerText = $"{operand1} + {operand2}";
                currentQuestionAnswer = operand1 + operand2;
                break;
            case "-":
                correctAnswerText = $"{operand1} - {operand2}";
                currentQuestionAnswer = operand1 - operand2;
                break;
            case "*":
                correctAnswerText = $"{operand1} * {operand2}";
                currentQuestionAnswer = operand1 * operand2;
                break;
            case "/":
                correctAnswerText = $"{operand1 * operand2} / {operand2}";
                currentQuestionAnswer = operand1;
                break;
            default:
                correctAnswerText = $"{operand1} + {operand2}";
                currentQuestionAnswer = operand1 + operand2;
                break;
        }

        Question.text = currentQuestionAnswer.ToString() + " ?";

        // Generate the correct answer
        CreateAnswer(CorrectAnswer, GetRandomSpawnPosition(), correctAnswerText);

        // Generate three wrong answers
        HashSet<string> answers = new HashSet<string> { correctAnswerText };
        while (answers.Count < 4)
        {
            string wrongAnswerText = GenerateWrongAnswer(maxNumber, selectedOperation, operand1, operand2);
            answers.Add(wrongAnswerText);
        }

        // Spawn the answers
        List<string> answerList = new List<string>(answers);
        foreach (string answer in answerList)
        {
            if (answer != correctAnswerText)
            {
                CreateAnswer(WrongAnswer, GetRandomSpawnPosition(), answer);
            }
        }
    }

    private string GenerateWrongAnswer(int maxNumber, string selectedOperation, int operand1, int operand2)
    {
        int wrongOperand1 = Random.Range(1, maxNumber);
        int wrongOperand2 = selectedOperation == "/" ? Random.Range(1, maxNumber / operand1) : Random.Range(1, maxNumber);
        int wrongResult;

        switch (selectedOperation)
        {
            case "+":
                wrongResult = wrongOperand1 + wrongOperand2;
                break;
            case "-":
                wrongResult = wrongOperand1 - wrongOperand2;
                break;
            case "*":
                wrongResult = wrongOperand1 * wrongOperand2;
                break;
            case "/":
                wrongResult = wrongOperand1 / wrongOperand2;
                break;
            default:
                wrongResult = wrongOperand1 + wrongOperand2;
                break;
        }

        // Check if the wrong result is the same as the correct answer
        if (wrongResult == currentQuestionAnswer)
        {
            // If it is the same, generate another wrong answer
            return GenerateWrongAnswer(maxNumber, selectedOperation, operand1, operand2);
        }
        else
        {
            return $"{wrongOperand1} {selectedOperation} {wrongOperand2}";
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        float randomX = Random.Range(dropTargetBounds.min.x, dropTargetBounds.max.x);
        float randomZ = Random.Range(dropTargetBounds.min.z, dropTargetBounds.max.z);
        return new Vector3(randomX, 2f, randomZ);
    }

    private void InitializeDropTargetBounds()
    {
        if (DropTarget != null)
        {
            Collider targetCollider = DropTarget.GetComponent<Collider>();
            if (targetCollider != null)
            {
                dropTargetBounds = targetCollider.bounds;
            }
            else
            {
                Debug.LogWarning("DropTarget does not have a Collider component.");
            }
        }
        else
        {
            Debug.LogWarning("DropTarget reference is not set. Please assign a target object in the inspector.");
        }
    }

    private void KeepSnakeWithinBounds()
    {
        if (DropTarget != null)
        {
            Vector3 position = transform.position;
            position.x = Mathf.Clamp(position.x, dropTargetBounds.min.x, dropTargetBounds.max.x);
            position.z = Mathf.Clamp(position.z, dropTargetBounds.min.z, dropTargetBounds.max.z);
            transform.position = position;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("CorrectAnswer"))
        {
            Destroy(other.gameObject);
            GrowSnake();
            RemoveAllAnswers();
            correctAnswersCount++; // Increase correct answer count
            if (correctAnswersCount >= CorrectAnswersToLevelUp)
            {
                IncreaseLevel(); // Check if it's time to increase level
            }
            GenerateQuestionAndAnswers();
        }
        else if (other.CompareTag("WrongAnswer"))
        {
            Destroy(other.gameObject);
            GoToMainMenu();
        }
        else if (other.CompareTag("eye")  && other.GetComponent<Collider>().enabled) 
        {
            Debug.Log("Snake head collided with body!");
            GoToMainMenu();
        }
    }

    private void RemoveAllAnswers()
    {
        foreach (GameObject answer in spawnedAnswers)
        {
            Destroy(answer);
        }
        spawnedAnswers.Clear();
    }

    private void UpdateLevelText()
    {
        LevelText.text = "Level: " + currentLevel.ToString();
    }

    public void IncreaseLevel()
    {
        currentLevel++;
        correctAnswersCount = 0; // Reset correct answer count
        UpdateLevelText();
        // You might want to reset some parameters or add more complexity to the game as the level increases
    }

    private void GoToMainMenu()
    {
        score = 0;
        SceneManager.LoadScene(0);
    }
}
