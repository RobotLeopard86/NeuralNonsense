namespace NeuralNonsense {
	public interface IAIGeneratorService {
		//Note: arrays must be pre-sized
		public Task<Room.AIContent> GenerateContent(uint playerCount);
	}

	public class FakeAIGeneratorService : IAIGeneratorService {
		public async Task<Room.AIContent> GenerateContent(uint playerCount) {
			Room.AIContent content = new Room.AIContent {
				r1 = new Room.AIContent.Pair[playerCount],
				r2 = new Room.AIContent.Pair[playerCount],
				r3Prompt = "",
				r3AiAnswers = new string[playerCount]
			};
			for(byte i = 0; i < playerCount; i++) {
				content.r1[i].prompt = "What an incredible prompt yes yes very good";
				content.r1[i].aiAnswer = "A very clever AI answer";
			}
			for(byte i = 0; i < playerCount; i++) {
				content.r2[i].prompt = "An even better prompt oh yes it's so good";
				content.r2[i].aiAnswer = "An AI answer that is very giga-brain";
			}
			content.r3Prompt = "A triumph of prompt-creation skill!";
			for(byte i = 0; i < playerCount; i++) {
				content.r3AiAnswers[i] = "Such a great AI answer, #" + i;
			}
			return content;
		}
	}
}