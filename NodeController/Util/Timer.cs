namespace NodeController.Util {
    using KianCommons;
    using System.Diagnostics;

    internal class Timer {
        private int counter;
        private Stopwatch sw = new();
        private string name;
        long last;
        long step;

        public Timer(string name, long step = 1000) {
            this.name = name;
            this.step = step;
        }

        public void Start() {
            sw.Start();
            counter++;
        }

        public void End() {
            sw.Stop();
            var ms = sw.ElapsedMilliseconds;
            if (ms > last + step) {
                last = ms;
                Log.Info($"{name}: {counter} times took {ms}ms: {(float)ms / counter}ms/iteration");
            }
        }
    }
}
