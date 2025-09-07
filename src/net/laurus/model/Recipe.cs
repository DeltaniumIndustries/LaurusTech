using System;
using XRL.World;

namespace LaurusTech.net.laurus.model
{
    [Serializable]
    public class Recipe : IComposite
    {
        public string Input;
        public string Output;
        public int Turns;
    }
}
