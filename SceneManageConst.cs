namespace NamPhuThuy.SceneManagement
{
    public static partial class SceneManageConst
    {
        public const string SCENE_BOOTSTRAP = "Bootstrap";
        public const string SCENE_LOADING = "Loading";
        public const string SCENE_MAIN_MENU = "MainMenu";
        public const string SCENE_GAME_PLAY = "GamePlay";
        public const string SCENE_SPLASH_SCREEN = "SplashScreen";
        public const string SCENE_DUMMY = "Dummy";

        public enum SceneName
        {
            None = 0,
            Bootstrap = 1,
            Loading = 2,
            MainMenu = 3,
            GamePlay = 4,
            SplashScreen = 5,
            Dummy = 999
        }
    }
}