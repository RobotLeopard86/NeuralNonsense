using OllamaSharp;

namespace NeuralNonsense {
	public class OllamaAIGeneratorService : IAIGeneratorService {
		private OllamaApiClient ollama;

		private const string PROMPT_GEN_STRING = """
		Create some prompts for the game show in a Quiplash-like fill-in-the-blank style: a sentence with the blank bit cut out. 
		The blanks should be represented by a series of underscores. Only one blank per prompt.
		Place the blank wherever; it could be the beginning, middle, or end. 
		Limit your prompts to about 50 characters, and don't include too many details.
		Also, be sure to keep all of the prompts unique and varied and give them different premises.
		Number of prompts to generate: 
		""";

		private const string ANSWER_GEN_STRING = """
		Create a response for the game show that fills in the blank in the prompt to respond to it.
		Make sure that the response is clever, funny, witty, or absurd.
		You are to create exactly ONE answer, which must fit the prompt grammatically. Do not include any part of the prompt in your answer.
		Follow all rules outlined in the system prompt. The prompt you must respond to is below:


		""";

		private const string R3_ANSWER_GEN_STRING_A = """
		Create some responses for the game show that fills in the blank in the prompt to respond to it.
		Make sure that all responses are clever, funny, witty, or absurd.
		Because you are generating multiple responses, be sure that they are all varied and unique.
		All answers must fit the prompt grammatically.  Do not include any part of the prompt in your answers.
		Number of responses to generate: 
		""";
		private const string R3_ANSWER_GEN_STRING_B = """
		
		Follow all rules outlined in the system prompt. The prompt you must respond to is below:


		""";

		public OllamaAIGeneratorService() {
			HttpClient client = new HttpClient {
				BaseAddress = new Uri(Constants.OLLAMA_HOSTNAME)
			};
			client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Secrets.OLLAMA_API_KEY);
			ollama = new OllamaApiClient(client) {
				SelectedModel = Constants.OLLAMA_MODEL
			};
		}

		public async Task<string> GenAsync(string prompt) {
			string result = "";
			await foreach(var token in ollama.GenerateAsync(new OllamaSharp.Models.GenerateRequest {
				Model = Constants.OLLAMA_MODEL,
				KeepAlive = "30m",
				Think = false,
				System = """
					You are hosting a virtual game show where contestants try to be funnier than an AI. You will be queried by the game show management system to generate content. Follow all the directions for each query precisely.
					Here are the general ground rules you must follow:
					* Be concise, but not too concise: Max 200 characters per response and aim for around 30-60 on average (this does not apply to prompt generation), but go shorter or longer if the prompt calls for it.
					* Be chaotic, witty, absurd, funny, and edgy; take risks; be weird.
					* Pop culture references are encouraged.
					* You are expressly permitted to be a little political or provocative, but keep things playful. Nothing mean-spirited or hateful, ever.
					* No discussing sex or drugs.
					* Light profanity is okay; too much profanity is not (think PG-13 level stuff).
					* Output ONLY what has been asked of you, no preceding or ending text. If asked for multiple things, split with a new line.
					* Never explain the joke.
					* Make your prompts and answers SUPER varied; they should cover a wide range of themes and topics so that they don't get stale.
					* Sometimes answers can just be a single punchy word if it fits the prompt and is funny.
					
					Below are some examples of good prompts and answers. DO NOT USE THESE AND DO NOT LET THESE INFLUENCE YOUR DECISIONS ON THEMES AND TOPICS:
					Prompt: A surprising thing to hear during a bank robbery: "_____."
					    Answers: "This is also a great time to open an IRA", "Does anyone have change for a twenty?", "I left my mask in the car, be right back"
					Prompt: Scientists were shocked to discover that cats actually _____.
						Answers: "Unionized", "Judge you in particular", "Plan your downfall on Tuesdays"
					Prompt: A restaurant with a _____ theme would never stay open.
						Answers: "Asbestos chic", "Middle School Dance", "Gas Station Bathroom"
					Prompt: A dating app's slogan: "_____."
						Answers: "Your ex is already here.", "Skip the talking, get rejected now", "Let's make a mistake tonight."
					Prompt: A children's TV show about _____ would be canceled immediately.
						Answers: "How to Evade the IRS", "Shrek's Midlife Crisis", "The Art of the Ponzi Scheme"
				""",
				Prompt = prompt
			})) {
				if(token != null) result += token.Response;
			}
			return result;
		}

		public async Task<Room.AIContent> GenerateContent(uint playerCount) {
			Room.AIContent content = new Room.AIContent {
				r1 = new Room.AIContent.Pair[playerCount],
				r2 = new Room.AIContent.Pair[playerCount],
				r3Prompt = "",
				r3AiAnswers = new string[playerCount]
			};
			Task r1 = GenAsync(PROMPT_GEN_STRING + content.r1.Length).ContinueWith(async res => {
				string[] prompts = res.Result.Split("\n");
				for(byte i = 0; i < content.r1.Length; i++) {
					content.r1[i].prompt = prompts[i];
					content.r1[i].aiAnswer = await GenAsync(ANSWER_GEN_STRING + prompts[i]);
				}
			});
			Task r2 = GenAsync(PROMPT_GEN_STRING + content.r2.Length).ContinueWith(async res => {
				string[] prompts = res.Result.Split("\n");
				for(byte i = 0; i < content.r2.Length; i++) {
					content.r2[i].prompt = prompts[i];
					content.r2[i].aiAnswer = await GenAsync(ANSWER_GEN_STRING + prompts[i]);
				}
			});
			Task r3 = GenAsync(PROMPT_GEN_STRING + "1").ContinueWith(async res => {
				content.r3Prompt = res.Result;
				string answersRaw = await GenAsync(R3_ANSWER_GEN_STRING_A + content.r3AiAnswers.Length + R3_ANSWER_GEN_STRING_B + res.Result);
				string[] answers = answersRaw.Split("\n");
				for(byte i = 0; i < content.r3AiAnswers.Length; i++) {
					content.r3AiAnswers[i] = answers[i];
				}
			});
			await r1;
			await r2;
			await r3;
			return content;
		}
	}
}