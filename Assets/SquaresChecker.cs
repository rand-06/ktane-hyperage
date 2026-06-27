using System.Collections.Generic;
using System.Linq;

public static class SquaresChecker
{
    private static bool valid(this List<string> rotations) =>
        rotations.Select(a => a[0]).Distinct().Count() == 4 && rotations[0][1] == rotations[1][0] &&
               rotations[1][1] == rotations[2][0] && rotations[2][1] == rotations[3][0] &&
               rotations[3][1] == rotations[0][0];

    private static List<string> getSquare(this List<string> rotations, int xCenter, int yCenter, int xRelative, int yRelative) =>
        new List<string>
        {
            rotations[(xCenter + xRelative) + (yCenter + yRelative)*4],
            rotations[(xCenter - yRelative) + (yCenter + xRelative)*4],
            rotations[(xCenter - xRelative) + (yCenter - yRelative)*4],
            rotations[(xCenter + yRelative) + (yCenter - xRelative)*4]
        };
    
    public static int squares(List<string> rotations) => 
        Enumerable.Range(0, 8).Select(xyz => valid(rotations.getSquare(xyz/2%2+1, xyz%2+1, 1, xyz/4))).Count(x=>x);

    public static List<int> getSquare(this List<string> rotations)
    {
        int x = -1, y = -1;
        int dx = -1, dy = -1;
        for (int i = 0; i < rotations.Count; i++)
        {
            x = i % 4;  y = i/4;
            
            if(x==0 || y==0 || x==3 || y==3) continue;
            if (valid(rotations.getSquare(x, y, 1, 0))) { dx = 1; dy = 0; break; }
            if (valid(rotations.getSquare(x, y, 1, 1))) { dx = 1; dy = 1; break; }
        }
        if (dx==-1 || dy==-1) return Enumerable.Repeat(-1,5).ToList();
        var ans =  new List<int>
        {
            (x) + (y) * 4,
            (x + dx) + (y + dy) * 4,
            (x - dy) + (y + dx) * 4,
            (x - dx) + (y - dy) * 4,
            (x + dy) + (y - dx) * 4,
        };
        var minIndex = ans.IndexOf(a => a == ans.Min(b => b));
        return ans.Take(1).Union(ans.Skip(minIndex)).Union(ans.Skip(1).Take(minIndex-1)).ToList();
    }
}