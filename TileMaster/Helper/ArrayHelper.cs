using System;
using System.Collections.Generic;

namespace TileMaster.Helper
{
    public static class ArrayHelper
    {
        public static List<int[,]> GetChunkUsingBlockCopy(int[,] array, int row, int column)
        {
            int chunkcount = (array.GetLength(0) * array.GetLength(1)) / (row * column);
            List<int[,]> chunkList = new List<int[,]>();
            int[,] chunk = new int[row, column];

            var byteLength = sizeof(int) * chunk.Length;
            for (int i = 0; i < chunkcount; i++)
            {
                chunk = new int[row, column];
                Buffer.BlockCopy(array, byteLength * i, chunk, 0, byteLength);

                chunkList.Add(chunk);
            }

            return chunkList;
        }
    }
}
