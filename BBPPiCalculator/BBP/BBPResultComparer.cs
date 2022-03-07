namespace BBP;

/// <summary>
///     Compare BBPResult objects to sort a list of BBPResult objects.
/// </summary>
public class BBPResultComparer : IComparer<BBPResult>
{
    public int Compare(BBPResult x, BBPResult y)
    {
        return x.Digit.CompareTo(value: y.Digit);
    }
}
