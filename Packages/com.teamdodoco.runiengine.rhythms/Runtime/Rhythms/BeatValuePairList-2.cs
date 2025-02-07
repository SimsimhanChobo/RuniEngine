#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RuniEngine.Rhythms
{
    [Serializable]
    public class BeatValuePairList<TValue, TPair> : ITypeList, IList<TPair>, IBeatValuePairList where TPair : IBeatValuePair<TValue>, new()
    {
        public BeatValuePairList(TValue defaultValue) => this.defaultValue = defaultValue;
        public BeatValuePairList(TValue defaultValue, IEnumerable<TPair> collection) : this(defaultValue) => serializableList = new List<TPair>(collection);

        public Type listType => typeof(TPair);
        [SerializeField] readonly List<TPair> serializableList = new List<TPair>();

        public int Count => serializableList.Count;

        public TValue? defaultValue { get; set; } = default;




        bool IList.IsFixedSize => ((IList)serializableList).IsFixedSize;
        bool IList.IsReadOnly => ((IList)serializableList).IsReadOnly;
        bool ICollection<TPair>.IsReadOnly => ((ICollection<TPair>)serializableList).IsReadOnly;
        bool ICollection.IsSynchronized => ((ICollection)serializableList).IsSynchronized;
        object? ICollection.SyncRoot => ((ICollection)serializableList).SyncRoot;



        object IList.this[int index]
        {
            get => serializableList[index];
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                try
                {
                    this[index] = (TPair)value;
                }
                catch (InvalidCastException)
                {
                    throw;
                }
            }
        }
        public virtual TPair this[int index] { get => serializableList[index]; set => serializableList[index] = value; }



        public TPair First()
        {
            if (Count > 0)
                return this[0];
            else
            {
                TPair pair = new TPair
                {
                    beat = double.MinValue,
                    value = defaultValue
                };

                return pair;
            }
        }

        public TPair Last()
        {
            if (Count > 0)
                return this[Count - 1];
            else
            {
                TPair pair = new TPair
                {
                    beat = double.MinValue,
                    value = defaultValue
                };

                return pair;
            }
        }



        //public TValue GetValue() => GetValue(RhythmManager.currentBeat, out _);
        public TValue? GetValue(double currentBeat) => GetValue(currentBeat, out _);

        TValue? tempValue = default;
        double tempBeat = 0;
        double? tempCurrentBeat = null;
        public virtual TValue? GetValue(double currentBeat, out double beat)
        {
            if (tempCurrentBeat != null && (double)tempCurrentBeat == currentBeat)
            {
                beat = tempBeat;
                return tempValue;
            }

            tempCurrentBeat = currentBeat;

            TValue? value;
            if (Count <= 0)
            {
                beat = 0;
                value = defaultValue;
            }
            else
            {
                TPair beatValuePair = this[GetValueIndexBinarySearch(currentBeat).Clamp(0)];

                beat = beatValuePair.beat;
                value = beatValuePair.value;
            }

            //isValueChanged = !((IEquatable<TValue>)tempValue).Equals(value);
            tempValue = value;
            tempBeat = beat;

            return value;
        }



        //public void FindValue(Predicate<TPair> match) => FindValue(RhythmManager.currentBeat, match, out _, out _);
        public void FindValue(double currentBeat, Predicate<TPair> match) => FindValue(currentBeat, match, out _, out _);

        public void FindValue(double currentBeat, Predicate<TPair> match, out double beat, out int index)
        {
            if (Count <= 0)
            {
                beat = 0;
                index = 0;

                return;
            }

            TPair firstPair = this[0];
            bool firstPairMatch = match(firstPair);
            if ((Count <= 1 && firstPairMatch) || (firstPair.beat >= currentBeat && firstPairMatch))
            {
                beat = this[0].beat;
                index = 0;

                return;
            }
            else
            {
                TPair lastPair = firstPair;
                int lastIndex = 0;
                for (int i = 0; i < Count; i++)
                {
                    TPair pair = this[i];
                    if (pair.beat > currentBeat && match(lastPair))
                    {
                        beat = lastPair.beat;
                        index = lastIndex - 1;

                        return;
                    }

                    lastPair = pair;
                    lastIndex = i;
                }

                beat = 0;
                index = 0;

                return;
            }
        }



        void IBeatValuePairList.Add(double beat) => Add(beat);
        public void Add(double beat = double.MinValue) => Add(new TPair() { beat = beat, value = defaultValue });
        public void Add(TValue? value) => Add(new TPair() { beat = double.MinValue, value = value });
        public void Add(double beat, TValue? value) => Add(new TPair() { beat = beat, value = value });

        int IList.Add(object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            try
            {
                Add((TPair)value);
            }
            catch (InvalidCastException)
            {
                throw;
            }

            return Count - 1;
        }
        public virtual void Add(TPair item)
        {
            if (Count > 0)
                serializableList.Insert(GetValueIndexBinarySearch(item.beat) + 1, item);
            else
                serializableList.Add(item);
        }

        void IList.Insert(int index, object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            try
            {
                Insert(index, (TPair)value);
            }
            catch (InvalidCastException)
            {
                throw;
            }
        }
        public virtual void Insert(int index, TPair item) => serializableList.Insert(index, item);



        void IList.Remove(object item)
        {
            if (IsCompatibleObject(item))
                Remove((TPair)item);
        }
        public virtual bool Remove(TPair item) => serializableList.Remove(item);

        public virtual void RemoveAt(int index) => serializableList.RemoveAt(index);



        public virtual void Clear() => serializableList.Clear();



        public virtual void Sort()
        {
            TPair[] list = this.OrderBy(x => x.beat).ToArray();
            serializableList.Clear();

            for (int i = 0; i < list.Length; i++)
            {
                TPair item = list[i];
                serializableList.Add(item);
            }
        }



        public virtual int GetValueIndexBinarySearch(double beat)
        {
            if (Count <= 0)
                return 0;
            else if (beat < this[0].beat)
                return 0;
            else if (beat >= this[Count - 1].beat)
                return Count - 1;

            int low = 0;
            int high = Count - 1;

            while (low < high)
            {
                int index = (low + high) / 2;
                if (this[index].beat <= beat)
                    low = index + 1;
                else
                    high = index;
            }

            return low - 1;
        }



        bool IList.Contains(object item) => IsCompatibleObject(item) && Contains((TPair)item);
        public bool Contains(TPair item) => serializableList.Contains(item);



        int IList.IndexOf(object item)
        {
            if (IsCompatibleObject(item))
                return IndexOf((TPair)item);

            return -1;
        }
        public int IndexOf(TPair item) => serializableList.IndexOf(item);



        void ICollection.CopyTo(Array array, int index) => CopyTo((TPair[])array, index);
        public virtual void CopyTo(TPair[] array, int arrayIndex) => serializableList.CopyTo(array, arrayIndex);

        IEnumerator IEnumerable.GetEnumerator() => serializableList.GetEnumerator();
        public IEnumerator<TPair> GetEnumerator() => serializableList.GetEnumerator();






        static bool IsCompatibleObject(object value)
        {
            if (value is TPair)
                return true;

            if (value == null)
                return default(TPair) == null;

            return false;
        }
    }
}
