using System;
using UnityEngine;

namespace LipSync
{
    public class ToeplitzMtrix
    {
        private float[,] data;

        private int size;

        public int Size
        {
            get
            {
                return size;
            }
        }

        public float this[int x, int y]
        {
            get
            {
                return data?[x, y] ?? 0;
            }
        }

        public ToeplitzMtrix(float[,] c)
        {
            data = c;
            size = (int)Mathf.Sqrt(c.Length);
        }

        public ToeplitzMtrix(float[] c)
        {
            size = c.Length;
            int n = size;
            data = new float[n, n];
            for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                if (i <= j)
                    data[i, j] = c[j - i];
                else
                    data[i, j] = c[i - j];
            }
        }

        public override string ToString()
        {
            var rt = "size: " + size;
            for (int i = 0; i < size; i++)
            {
                rt += "\n";
                for (int j = 0; j < size; j++) rt += this[i, j].ToString("f2") + "\t";
            }
            return rt;
        }

        // 求逆
        public float[,] Inverse()
        {
            float dMatrixValue = MatrixValue(data, size);
            if (dMatrixValue == 0) return null;
            float[,] dReverseMatrix = new float[size, 2 * size];
            float x, c;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < 2 * size; j++)
                {
                    if (j < size)
                        dReverseMatrix[i, j] = data[i, j];
                    else
                        dReverseMatrix[i, j] = 0;
                }
                dReverseMatrix[i, size + i] = 1;
            }
            for (int i = 0, j = 0; i < size && j < size; i++, j++)
            {
                if (dReverseMatrix[i, j] == 0)
                {
                    int m = i;
                    for (; data[m, j] == 0; m++) ;
                    if (m == size)
                        return null;
                    else
                    {
                        // Add i-row with m-row         
                        for (int n = j; n < 2 * size; n++) dReverseMatrix[i, n] += dReverseMatrix[m, n];
                    }
                }
                // Format the i-row with "1" start   
                x = dReverseMatrix[i, j];
                if (x != 1)
                {
                    for (int n = j; n < 2 * size; n++)
                        if (dReverseMatrix[i, n] != 0)
                            dReverseMatrix[i, n] /= x;
                }
                // Set 0 to the current column in the rows after current row  
                for (int s = size - 1; s > i; s--)
                {
                    x = dReverseMatrix[s, j];
                    for (int t = j; t < 2 * size; t++) dReverseMatrix[s, t] -= (dReverseMatrix[i, t] * x);
                }
            }
            // Format the first matrix into unit-matrix  
            for (int i = size - 2; i >= 0; i--)
            {
                for (int j = i + 1; j < size; j++)
                    if (dReverseMatrix[i, j] != 0)
                    {
                        c = dReverseMatrix[i, j];
                        for (int n = j; n < 2 * size; n++) dReverseMatrix[i, n] -= (c * dReverseMatrix[j, n]);
                    }
            }
            float[,] dReturn = new float[size, size];
            for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
                dReturn[i, j] = dReverseMatrix[i, j + size];
            return dReturn;
        }


        private float MatrixValue(float[,] MatrixList, int Level)
        {
            float[,] dMatrix = new float[Level, Level];
            for (int i = 0; i < Level; i++)
            for (int j = 0; j < Level; j++)
                dMatrix[i, j] = MatrixList[i, j];
            float c, x;
            int k = 1;
            for (int i = 0, j = 0; i < Level && j < Level; i++, j++)
            {
                if (dMatrix[i, j] == 0)
                {
                    int m = i;
                    for (; dMatrix[m, j] == 0; m++) ;
                    if (m == Level)
                        return 0;
                    else
                    {
                        // Row change between i-row and m-row           
                        for (int n = j; n < Level; n++)
                        {
                            c = dMatrix[i, n];
                            dMatrix[i, n] = dMatrix[m, n];
                            dMatrix[m, n] = c;
                        }
                        // Change value pre-value      
                        k *= (-1);
                    }
                }
                // Set 0 to the current column in the rows after current row   
                for (int s = Level - 1; s > i; s--)
                {
                    x = dMatrix[s, j];
                    for (int t = j; t < Level; t++) dMatrix[s, t] -= dMatrix[i, t] * (x / dMatrix[i, j]);
                }
            }
            float sn = 1;
            for (int i = 0; i < Level; i++)
            {
                if (dMatrix[i, i] != 0)
                    sn *= dMatrix[i, i];
                else
                    return 0;
            }
            return k * sn;
        }

        public float[] Dot(float[] v)
        {
            if (data == null)
            {
                throw new System.Exception("matrix is nil");
            }
            if (size != v.Length)
            {
                throw new System.Exception("length is not equal matrix size");
            }
            float[] ret = new float[size];

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    ret[i] += this[i, j] * v[j];
                }
            }
            return ret;
        }
    }
}
