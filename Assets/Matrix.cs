using System;
using System.Collections.Generic;
using System.Linq;

public static class Matrix
{
    private static List<int> discard(this List<int> matrix, int z, int size) => matrix.Where((_, i) => !(i % size == z || i / size == 0)).ToList();
    
    public static long det(this List<int> matrix, int size = 4) => size == 1 ? matrix[0] : 
        Enumerable.Range(0, size).Select(i => matrix[i] * (long)Math.Pow(-1, i) * det(matrix.discard(i, size), size-1)).Sum();
    
    public static string getBase4Determinant(this long det) => base4(Math.Abs(det) / 1000000000000L, Math.Abs(det) % 1000000000000L);

    private static string base4(long whole, long fraction)
    {
        var digits = new List<char>();
        do {
            digits.Add((char)('0' + whole % 4));
            whole /= 4;
        } while (whole > 0);
        digits.Reverse();
        digits.Add('.');
        decimal current = fraction / 1000000000000M;
        for (int i = 0; i < 3; i++)
        {
            current *= 4;
            digits.Add((char)('0' + (int)current));
            current -= (int)current;
        }
        return digits.Join("");
    }
}