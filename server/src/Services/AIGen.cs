namespace NeuralNonsense {
	public interface IAIGeneratorService {
		//Note: arrays must be pre-sized
		public void GenerateContent(ref Room.AIContent content);
	}

	public class FakeAIGeneratorService : IAIGeneratorService {
		public void GenerateContent(ref Room.AIContent content) {
			for(byte i = 0; i < content.r1.Length; i++) {
				content.r1[i].prompt = "What an incredible prompt yes yes very good";
				content.r1[i].aiAnswer = "A very clever AI answer";
			}
			for(byte i = 0; i < content.r2.Length; i++) {
				content.r2[i].prompt = "An even better prompt oh yes it's so good";
				content.r2[i].aiAnswer = "An AI answer that is very giga-brain";
			}
			content.r3Prompt = "A triumph of prompt-creation skill!";
			for(byte i = 0; i < content.r3AiAnswers.Length; i++) {
				content.r3AiAnswers[i] = "Such a great AI answer, #" + i;
			}
		}
	}
}