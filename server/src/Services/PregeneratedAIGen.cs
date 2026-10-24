using System.Reflection;
using System.Text.Json;

namespace NeuralNonsense {
	public class PregeneratedAIGeneratorService : IAIGeneratorService {
		private record PregenData(string prompt, string[] answers);
		private readonly List<PregenData> sourceData;

		public PregeneratedAIGeneratorService() {
			//Get the content JSON
			Assembly assembly = Assembly.GetExecutingAssembly();
			string resourceName = "NeuralNonsense.Server.src.Services.pregen_content.json";
			using Stream stream = assembly.GetManifestResourceStream(resourceName) ?? throw new FileNotFoundException($"Embedded resource {resourceName} not found.");

			//Deserialize it
			using StreamReader reader = new StreamReader(stream);
			string json = reader.ReadToEnd();
			sourceData = JsonSerializer.Deserialize<List<PregenData>>(json) ?? throw new InvalidOperationException("Failed to deserialize pregen_content.json");
		}

		public async Task<Room.AIContent> GenerateContent(uint playerCount) {
			//Prepare
			Room.AIContent content = new Room.AIContent {
				r1 = new Room.AIContent.Pair[playerCount],
				r2 = new Room.AIContent.Pair[playerCount],
				r3Prompt = "",
				r3AiAnswers = new string[playerCount]
			};

			//Shuffle the source data for *random magic*
			PregenData[] pool = sourceData.ToArray();
			Random.Shared.Shuffle(pool);
			int poolIndex = 0;

			//Round 1 content
			for(int i = 0; i < content.r1.Length; i++) {
				//Get next pool entry
				if(poolIndex >= pool.Length) break;
				PregenData data = pool[poolIndex++];

				//Shuffle the answers
				string[] shuffledAnswers = (string[])data.answers.Clone();
				Random.Shared.Shuffle(shuffledAnswers);

				//Make a new content pair
				content.r1[i] = new Room.AIContent.Pair {
					prompt = data.prompt,
					aiAnswer = shuffledAnswers[0]
				};
			}

			//Round 2 content (same thing again so no comments)
			for(int i = 0; i < content.r2.Length; i++) {
				if(poolIndex >= pool.Length) break;
				PregenData data = pool[poolIndex++];
				string[] shuffledAnswers = (string[])data.answers.Clone();
				Random.Shared.Shuffle(shuffledAnswers);
				content.r2[i] = new Room.AIContent.Pair {
					prompt = data.prompt,
					aiAnswer = shuffledAnswers[0]
				};
			}

			//Round 3 content
			if(poolIndex < pool.Length) {
				//Choose a random prompt
				PregenData data = pool[poolIndex++];
				content.r3Prompt = data.prompt;

				//Shuffle answers
				string[] shuffledAnswers = (string[])data.answers.Clone();
				Random.Shared.Shuffle(shuffledAnswers);

				//Get as many answers as we need with fallback
				for(int i = 0; i < content.r3AiAnswers.Length; i++) {
					if(i < shuffledAnswers.Length) {
						content.r3AiAnswers[i] = shuffledAnswers[i];
					} else {
						content.r3AiAnswers[i] = "Oh noes! It seems I've run out of things to say! Um... beep boop haha funny?";
					}
				}
			}

			return content;
		}
	}
}
