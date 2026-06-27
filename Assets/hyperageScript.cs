using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Random = UnityEngine.Random;

public class hyperageScript : MonoBehaviour {

	public KMAudio Audio;
	public AudioClip[] audioClips;
	public KMSelectable[] buttons;
	public MeshRenderer[] buttonsRenderer;
	public TextMesh[] buttonTexts;
	public TextMesh[] coordinateTexts;
	public TextMesh centerText;
	public GameObject[] orbs;
	
	public bool ModuleSolved;
	private static int ModuleIdCounter = 1;
	private int ModuleId;

	private readonly List<string> rotations = new List<string>{ "XY", "XZ", "XW", "YX", "YZ", "YW", "ZX", "ZY", "ZW", "WX", "WY", "WZ" };
	private bool rotationIsInProgress;
	private string currentRotation = "";
	private int currentButton = -1;
	private List<string> buttonRotations;
	private List<int> square;
	private const string base16 = "0123456789ABCDEF";
	private List<List<int>> buttonCoordinates;
	private List<int> buttonUnknownCoordinateIndices;
	private List<List<string>> coordinatesStrings;
	private bool submissionMode;
	private string answerString = "";
	private bool animationIsInProgress;
	
	private static Vector3 getCoordinate(float rotation, float magnitude, float y = -1)
	{
		return new Vector3(
			(float)Math.Sin(rotation*Math.PI*2) * magnitude, y, (float)Math.Cos(rotation*Math.PI*2) * magnitude
		);
	}
	
	private static Vector3 coordinateFromIndex(int index) => new Vector3(
		(index & 1) == 1?1f:-1f, 
		(index & 2) == 2?1f:-1f, 
		(index & 4) == 4?1f:-1f
		) * ((index & 8) == 8? 1f : .5f);

	private int getIndexAfterRotation(int initialIndex, string rotation)
	{
		bool[] axes = new bool[4]; //[X, Y, Z, W]-ordered.
		for (int i = 0; i < axes.Length; i++)
		{
			axes[i] = initialIndex % 2 == 1;
			initialIndex /= 2;
		}
		int axis2 = "XYZW".IndexOf(rotation[0]);
		int axis1 = "XYZW".IndexOf(rotation[1]);
		bool bool1 = axes[axis1];
		bool bool2 = axes[axis2];
		axes[axis1] = bool2;
		axes[axis2] = !bool1;
		int ans = 0;
		for (int i = axes.Length; i > 0 ; i--)
		{
			ans *= 2;
			ans += axes[i-1]? 1 : 0;
		}

		return ans;
	}
	
	string divideBy1000(int num) => Math.Abs(num).ToString("D4").Insert(1, ".");
	
	void returnToOriginalPositions() { for (int i = 0; i < orbs.Length; i++) orbs[i].transform.localPosition = coordinateFromIndex(i); }
	
	private IEnumerator rotate()
	{
		while (!ModuleSolved && !submissionMode)
		{
			rotationIsInProgress = true;
			if (currentButton != -1)
			{
				centerText.text = base16[currentButton].ToString();
				for (int i = 0; i < 4; i++) coordinateTexts[i].text = coordinatesStrings[currentButton][i];
			}
			else
			{
				centerText.text = "";
				for (int i = 0; i < 4; i++) coordinateTexts[i].text = "";
			}

			if (currentRotation != "")
			{
				Vector3[] initialPositions = new Vector3[orbs.Length];
				Vector3[] positionsAfterRotation = new Vector3[orbs.Length];
				for (int i = 0; i < orbs.Length; i++)
				{
					initialPositions[i] = coordinateFromIndex(i);
					positionsAfterRotation[i] = coordinateFromIndex(getIndexAfterRotation(i, currentRotation));
				}

				float timePassed = 0f;
				while (timePassed < .5f)
				{
					timePassed += .05f;
					yield return new WaitForSeconds(.05f);
					for (int i = 0; i < orbs.Length; i++)
						orbs[i].transform.localPosition =
							Vector3.Lerp(initialPositions[i], positionsAfterRotation[i], timePassed/.5f);
				}

				returnToOriginalPositions();
			}
			yield return new WaitForSeconds(.5f);
			rotationIsInProgress = false;
		}
	}
	
	void buttonInit()
	{
		for (int ii = 0; ii < buttons.Length; ii++)
		{
			int i = ii;
			int x = i%4, y=i/4;
			float length = (float)Math.Sqrt((x - 2.5) * (x - 2.5) + (y - 2.5) * (y - 2.5));
			buttonsRenderer[i].material.color = Color.Lerp(new Color(0f,0f,0.7f), new Color(0f,0f,0.2f), length/(float)Math.Sqrt(12.5));
			buttonTexts[i].text = "";
			buttons[i].OnHighlight += delegate
			{
				if (submissionMode) return;
				currentRotation = buttonRotations[i];
				currentButton = i;
			};
			buttons[i].OnHighlightEnded += delegate
			{
				if (submissionMode) return;
				currentButton = -1;
				currentRotation = "";
			};
			buttons[i].OnInteract += delegate
			{
				if (ModuleSolved || animationIsInProgress) return false;
				pressButton(i);
				return false;
			};
		}
	}

	IEnumerator stageAnimation()
	{
		animationIsInProgress = true;
		while (rotationIsInProgress) yield return new WaitForSeconds(.1f);
		centerText.text = "";
		for (int i = 0; i < 4; i++) coordinateTexts[i].text = "";
		float timer = 0f;
		while (timer < 1f)
		{
			for (int i = 0; i < orbs.Length; i++) 
				orbs[i].transform.localPosition = Vector3.Lerp(coordinateFromIndex(i), new Vector3(0, -1f, 0),timer);
			timer+=Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}

		centerText.color = Color.clear;
		centerText.text = "0";
		for (int i = 0; i < 16; i++) buttonTexts[i].color = Color.clear;
		drawButtonsOnY(0);
		timer = 0f;
		while (timer < 1f)
		{
			foreach (var c in buttonTexts) 
				c.color = Color.Lerp(new Color(1,1,1,0), new Color(1,1,1,.5f), timer);
			centerText.color = Color.Lerp(new Color(1,1,1,0), new Color(1,1,1,.75f), timer);
			timer+=Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
		animationIsInProgress = false;
	}

	int mod(int x, int m) => (x % m + m) % m;
	
	void drawButtonsOnY(int y)
	{
		for (int i = 0; i < 4; i++) buttonTexts[i + 4*y].text             = "0123"[i].ToString();
		for (int i = 0; i < 4; i++) buttonTexts[i + mod(4*(y+1),16)].text = "SUBM"[i].ToString();
		for (int i = 0; i < 4; i++) buttonTexts[i + mod(4*(y-1),16)].text = "RST."[i].ToString();
		for (int i = 0; i < 4; i++) buttonTexts[i + mod(4*(y+2),16)].text = "";
	}

	private IEnumerator rotateInCircles()
	{
		float timer = 0f;
		while (true)
		{
			for (int i = 0; i < 16; i++)
			{
				if (i * 22.5f >= timer * 120) continue;
				orbs[i].transform.localPosition = getCoordinate(
						(timer * 120 - i * 22.5f) / 360f, 2.5f * 
						(i * 22.5f < timer * 120 && timer * 120 < i * 22.5f + 360
							? (timer * 120 - i * 22.5f) / 360f:1f)
					);
			}
			timer+=Time.deltaTime;
			yield return new WaitForEndOfFrame();
			if (timer > 3f*5) timer -= 3f;
		}
	}

	private IEnumerator solve()
	{
		ModuleSolved = true;

		float timer = 0f;
		while (timer < 1f)
		{
			foreach (var c in buttonTexts) 
				c.color =      Color.Lerp(new Color(1,1,1,.50f), new Color(1,1,1,0), timer);
			centerText.color = Color.Lerp(new Color(1,1,1,.75f), new Color(1,1,1,0), timer);
			timer+=Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
		centerText.text = "";
		centerText.color = new Color(1, 1, 1, .75f);
		foreach (var c in buttonTexts)
		{
			c.text = "";
			c.color = new Color(1, 1, 1, .50f);
		}
		
		for (int i = 0; i < 16; i++)
		{
			buttonTexts[i].text = "HYPERAGESOLVED:D"[i].ToString();
			Audio.HandlePlayGameSoundAtTransform(KMSoundOverride.SoundEffect.SelectionTick,transform);
			yield return new WaitForSeconds(.1f);
		}
		
		yield return new WaitForSeconds(.5f);
		GetComponent<KMBombModule>().HandlePass();
		Audio.HandlePlaySoundAtTransform(audioClips[1].name, transform);
		StartCoroutine(rotateInCircles());
	}

	private IEnumerator strike()
	{
		yield return new WaitForSeconds(.1f);
		
		animationIsInProgress = true;
		float timer = 0;
		while (timer < 1f)
		{
			foreach (var c in buttonTexts) 
				c.color =      Color.Lerp(new Color(1,1,1,.50f), new Color(1,1,1,0), timer);
			centerText.color = Color.Lerp(new Color(1,1,1,.75f), new Color(1,1,1,0), timer);
			timer+=Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
		centerText.text = "";
		centerText.color = new Color(1, 1, 1, .75f);
		foreach (var c in buttonTexts) c.text = "";
		drawButtonsOnY(0);
		timer = 0;
		while (timer < 1f)
		{
			for (int i = 0; i < orbs.Length; i++)
				orbs[i].transform.localPosition = Vector3.Lerp(new Vector3(0, -1f, 0), coordinateFromIndex(i),timer);
			timer+=Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
		animationIsInProgress = false;
		GetComponent<KMBombModule>().HandleStrike();
		submissionMode = false;
		currentButton = -1;
		currentRotation = "";
		StartCoroutine(rotate());
	}
	
	void pressButton(int index)
	{
		Audio.HandlePlaySoundAtTransform(audioClips[0].name, transform);
		if (!submissionMode)
		{
			submissionMode = true;
			currentRotation = "";
			currentButton = 15;
			StartCoroutine(stageAnimation());
			return;
		}
		if (animationIsInProgress) return;
		int yPrevious = currentButton / 4;
		int yCurrent = index / 4;
		if (yPrevious == yCurrent)
		{
			if (index % 4 == 3) centerText.text += ".";
			else
			{
				centerText.text = "0";
				drawButtonsOnY(0);
				currentButton = 15;
			}
		}
		else if (mod(yCurrent - yPrevious,4) == 2)
		{
			StartCoroutine(centerText.text == answerString ? solve() : strike());
		}
		else if (mod(yCurrent - yPrevious,4) == 1)
		{
			currentButton = index;
			if (centerText.text == "0") centerText.text = (index%4).ToString();
			else centerText.text += (index%4).ToString();
			drawButtonsOnY(mod(yCurrent+1,4));
		}
	}
	
	void generateButtonRotations()
	{
		// Whoever is reading this, I AM GENUINELY SORRY FOR THIS PIECE OF CODE.
		// I don't know how to make the config, and I don't really want to spend way too much time on that,
		// so we have the bruteforce here. If you're not lucky, this function may lag, but I just hope that it won't.
		do buttonRotations = Enumerable.Range(0, buttons.Length).Select(_ => rotations.PickRandom()).ToList();
		while (SquaresChecker.squares(buttonRotations) != 1);
		square = buttonRotations.getSquare();
		Debug.LogFormat("[Hyperage #{0}] Center is {1}. Corners: {2}", ModuleId, base16[square[0]],
			square.GetRange(1,4).Select(a => base16[a]).Join(", "));
	}

	void generateButtonCoordinates()
	{
		buttonCoordinates = Enumerable.Range(0, buttons.Length)
			.Select(_ => Enumerable.Range(0, 4).Select(__ => Random.Range(1, 10000)).ToList()).ToList();
		buttonUnknownCoordinateIndices = Enumerable.Range(0, buttons.Length)
			.Select(_ => Random.Range(0, 4)).ToList();
		coordinatesStrings = Enumerable.Range(0, buttons.Length).Select(i =>
			Enumerable.Range(0, 4).Select(j => 
				j==buttonUnknownCoordinateIndices[i]?"?.???":divideBy1000(buttonCoordinates[i][j])
			).ToList()).ToList();
	}

	void getAns()
	{
		List<int> matrix = Enumerable.Range(0, 16).Select(x =>
		{
			int i = x / 4, j = x % 4;
			return buttonUnknownCoordinateIndices[square[i + 1]] == j
				? buttonUnknownCoordinateIndices[square[0]] == i
					? 1000
					: buttonCoordinates[square[0]][i]
				: buttonCoordinates[square[i + 1]][j];
		}).ToList();
		Debug.LogFormat("[Hyperage #{0}] Matrix: \n[Hyperage #{0}] {1}\t{2}\t{3}\t{4}\n[Hyperage #{0}] {5}\t{6}\t{7}\t{8}\n[Hyperage #{0}] {9}\t{10}\t{11}\t{12}\n[Hyperage #{0}] {13}\t{14}\t{15}\t{16}", ModuleId,
			divideBy1000(matrix[ 0]), divideBy1000(matrix[ 4]), divideBy1000(matrix[ 8]), divideBy1000(matrix[12]), 
			divideBy1000(matrix[ 1]), divideBy1000(matrix[ 5]), divideBy1000(matrix[ 9]), divideBy1000(matrix[13]),  
			divideBy1000(matrix[ 2]), divideBy1000(matrix[ 6]), divideBy1000(matrix[10]), divideBy1000(matrix[14]), 
			divideBy1000(matrix[ 3]), divideBy1000(matrix[ 7]), divideBy1000(matrix[11]), divideBy1000(matrix[15])
			);
		answerString = matrix.det().getBase4Determinant();
		Debug.LogFormat("[Hyperage #{0}] Answer is: {1}",ModuleId,answerString);
	}
	
	void Start () {
		ModuleId = ModuleIdCounter++;
		generateButtonRotations();
		generateButtonCoordinates();
		buttonInit();
		foreach (var t in orbs) t.GetComponent<MeshRenderer>().material.color = new Color (.3f,.5f,.7f);
		StartCoroutine(rotate());
		getAns();
	}

#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"Use !{0} hover # to hover over the button 0-F. Use !{0} submit <number> to submit the answer.";
#pragma warning restore 414
	
	public IEnumerator ProcessTwitchCommand(string Command)
	{
		yield return null;
		Command = Command.ToUpperInvariant();
		if (Regex.IsMatch(Command, "^HOVER ([0-9A-F])$"))
		{
			char button = Regex.Match(Command, "^HOVER ([0-9A-F])$").Groups[1].Value[0];
			int index = base16.IndexOf(button);
			if (currentButton != -1) buttons[currentButton].OnHighlightEnded();
			buttons[index].OnHighlight();
			yield break;
		}
		if (Regex.IsMatch(Command, "^SUBMIT ([0-3]+\\.[0-3]{3})$"))
		{
			if (!submissionMode)
			{
				buttons[0].OnInteract();
				while (animationIsInProgress) yield return new WaitForEndOfFrame();
			}
			string ans = Regex.Match(Command, "^SUBMIT ([0-3]+\\.[0-3]{3})$").Groups[1].Value;
			yield return ans == answerString ? solve() : strike();
			yield break;
		}
		yield return "sendtochaterror Invalid command.";
	}

	public IEnumerator TwitchHandleForcedSolve()
	{
		yield return null;
		yield return ProcessTwitchCommand("submit " + answerString);
	}
}
