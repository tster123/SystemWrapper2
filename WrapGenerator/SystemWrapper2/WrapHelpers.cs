namespace SystemWrapper2;

internal static class WrapHelpers
{
    public static TOut[] WrapArray<TIn, TOut>(TIn[] val, Func<TIn, TOut> wrapElem) 
        where TIn : class
        where TOut : class
    {
        if (val == null) return null;
        TOut[] ret = new TOut[val.Length];
        for (int i = 0; i < val.Length; i++)
        {
            TIn a = val[i];
            if (a != null)
            {
                ret[i] = wrapElem(a);
            }
        }
        return ret;
    }

    public static IEnumerable<TOut> WrapEnumerable<TIn, TOut>(IEnumerable<TIn> val, Func<TIn, TOut> wrapElem)
        where TIn : class
        where TOut : class
    {
        if (val == null) return null;
        return val.Select(e => e == null ? null : wrapElem(e));
    }
}