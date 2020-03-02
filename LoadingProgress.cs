using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataViewer
{
    public class LoadingProgress
    {
        private Mutex progressLock = new Mutex();
        private int end;
        private int step;
        private int current;
        private MainWindow window;
        public LoadingProgress(MainWindow windowInst)
        {
            window = windowInst;
        }

        public int Get()
        {
            return current;
        }
        public void Progress(int value)
        {
            lock (progressLock)
            {
                current += value;

                if (current % step == 0)
                {
                    window.progressValue++;
                    window.NotifyPropertyChange("ProgressValue");
                }
            }
        }

        public void Set(int value)
        {
            lock (progressLock)
            {
                current = value;

                if (current % step == 0)
                {
                    window.progressValue = current / step;
                    window.NotifyPropertyChange("ProgressValue");
                }
            }
        }

        public void SetEnd(int value)
        {
            lock (progressLock)
            {
                end = value;
                step = end / 100;
            }
        }

        public void Reset()
        {
            lock (progressLock)
            {
                current = 0;
                end = 0;
                step = 0;
            }
        }
    }
}
