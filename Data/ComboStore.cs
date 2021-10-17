
using System.Collections.Generic;

namespace GCDTracker.Data
{
    public static class ComboStore
    {
        public static Dictionary<uint, uint[][]> COMBOS = new Dictionary<uint, uint[][]>(){
            //Gladiator
            {1,new uint[][] {
                new uint[] {9,15,21},
                new uint[] {7383,16457}
            }},
            //Paladin
            {19,new uint[][] {
                new uint[] {9,15,3638},
                new uint[] {9,15,3538},
                new uint[] {7381,16457},
            }}
        };
    }
}
