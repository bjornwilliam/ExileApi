using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Willplug
{
    public class FixedSizedQueue : ConcurrentQueue<float>
    {
        private readonly object syncObject = new object();

        public int Size { get; private set; }

        public FixedSizedQueue(int size)
        {
            Size = size;
        }

        public float GetSum()
        {
            float sum = 0;
            foreach (var measurement in this)
            {
                sum += measurement;
            }
            return sum;
        }

        public new void Enqueue(float obj)
        {
            base.Enqueue(obj);
            lock (syncObject)
            {
                while (base.Count > Size)
                {
                    float outObj;
                    base.TryDequeue(out outObj);
                }
            }
        }
    }
}
