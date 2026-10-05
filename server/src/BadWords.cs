namespace NeuralNonsense {
	public interface BadWordChecker {
		public bool IsOffensive(string input);
	}

	public sealed class FakeBadWordChecker : BadWordChecker {
		public bool IsOffensive(string input) {
			return false;
		}
	}
}