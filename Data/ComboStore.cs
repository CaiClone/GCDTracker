
using System.Collections.Generic;

namespace GCDTracker.Data
{
    public static class ComboStore
    {
        public static Dictionary<uint, int[][]> COMBOS = new Dictionary<uint, int[][]>(){
            //Gladiator
            {1,new int[][] {
                new int[] {9,15,21},
                new int[] {7383,16457}
            }},
            //Paladin
            {19,new int[][] {
                new int[] {9,15,3539},
                new int[] {9,15,3638},
                new int[] {7383,16457},
            }}
        };
    }
}
