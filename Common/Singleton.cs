namespace HiHi {
	public abstract class Singleton<T> where T : new() {
		public static bool Exists => instance != null;
		public static T Instance {
			get {
				if(instance == null){
					instance = new T();
				}
				
				return instance;
			}
			set { 
				instance = value; 
			}
		}
		private static T instance;
	}
}
