using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void NaturalSort(SqlString midLookupSproc, SqlString wrapperLookupSproc, SqlString sortSproc, SqlInt32 id)
    {
        //Start the process by looking up the current sort value
        String sortString = getInitalSortString(wrapperLookupSproc.Value, id.Value);

        //Now create the first sort wrapper and start working out where this needs to go
        SortWrapper sw = new SortWrapper(sortString, id.Value, 0);
        recurseMidPoint(midLookupSproc.Value, sortSproc.Value, null, null, sw);
    }

    //Use a stored proc to get the initial sort string
    public static string getInitalSortString(string storedProc, int id)
    {
        using (SqlConnection conn = new SqlConnection("context connection=true"))
        {
            using (SqlCommand cmd = new SqlCommand(storedProc, conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                conn.Open();

                SqlDataReader reader = cmd.ExecuteReader();
                try
                {
                    if (reader.Read())
                    {
                        return reader[0].ToString();
                    }
                }
                finally
                {
                    reader.Close();
                    conn.Close();
                }
            }
        }
        return "";
    }

    //Recursively called method to work out the midpoint and re-order things accordingly
    private static void recurseMidPoint(String midLookupSproc, String sortSproc, SortWrapper lowerBounds, SortWrapper upperBounds, SortWrapper newItem)
    {
        SortWrapper midPoint = null;

        using (SqlConnection conn = new SqlConnection("context connection=true"))
        {
            using (SqlCommand midPointCommand = new SqlCommand(midLookupSproc, conn))
            {
                midPointCommand.CommandType = CommandType.StoredProcedure;
                midPointCommand.Parameters.Add("@id", SqlDbType.Int).Value = newItem.id;

                if (lowerBounds == null)
                {
                    midPointCommand.Parameters.Add("@lower_order", SqlDbType.Int).Value = DBNull.Value;
                }
                else
                {
                    midPointCommand.Parameters.Add("@lower_order", SqlDbType.Int).Value = lowerBounds.order;
                }

                if (upperBounds == null)
                {
                    midPointCommand.Parameters.Add("@higher_order", SqlDbType.Int).Value = DBNull.Value;
                }
                else
                {
                    midPointCommand.Parameters.Add("@higher_order", SqlDbType.Int).Value = upperBounds.order;
                }
                conn.Open();

                SqlDataReader reader = midPointCommand.ExecuteReader();
                try
                {
                    if (reader.Read())
                    {
                        midPoint = new SortWrapper();
                        midPoint.sortString = reader[0].ToString();
                        midPoint.id = (int)reader[1];
                        midPoint.order = (int)reader[2];
                    }
                }
                finally
                {
                    reader.Close();
                    conn.Close();
                }
            }
        }

        if (midPoint == null)
        {
            if (upperBounds == null && lowerBounds == null)
            {
                newItem.order = 0;
            }
            else if (upperBounds == null)
            {
                newItem.order = lowerBounds.order + 1;
            }
            else
            {
                newItem.order = upperBounds.order;
            }
            //Actually do the sorting
            doSorting(sortSproc, newItem.id, newItem.order);
        }
        else
        {
            //Do the natural order comparison
            int result = compareNatural(midPoint.sortString, newItem.sortString, false);

            if (result > 0)
            {
                recurseMidPoint(midLookupSproc, sortSproc, lowerBounds, midPoint, newItem);
            }
            else if (result < 0)
            {
                recurseMidPoint(midLookupSproc, sortSproc, midPoint, upperBounds, newItem);
            }
            else
            {
                newItem.order = midPoint.order + 1;

                //Actually do the sorting
                doSorting(sortSproc, newItem.id, newItem.order);

            }
        }
    }

    //Calls the stored procedure to actually do the sorting
    public static void doSorting(string sortSproc, int id, int order)
    {
        using (SqlConnection conn = new SqlConnection("context connection=true"))
        {
            using (SqlCommand cmd = new SqlCommand(sortSproc, conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                cmd.Parameters.Add("@sort", SqlDbType.Int).Value = order;
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }

    //A C# re-implementation of the natural language comparator originally in Java
    public static int compareNatural(string s, string t, bool caseSensitive)
    {
        int sIndex = 0;
        int tIndex = 0;

        int sLength = s.Length;
        int tLength = t.Length;

        while (true)
        {
            if (sIndex == sLength && tIndex == tLength)
            {
                return 0;
            }
            if (sIndex == sLength)
            {
                return -1;
            }
            if (tIndex == tLength)
            {
                return 1;
            }

            char sChar = s[sIndex];
            char tChar = t[tIndex];

            bool sCharIsDigit = Char.IsDigit(sChar);
            bool tCharIsDigit = Char.IsDigit(tChar);

            if (sCharIsDigit && tCharIsDigit)
            {
                int sLeadingZeroCount = 0;
                while (sChar == '0')
                {
                    ++sLeadingZeroCount;
                    ++sIndex;
                    if (sIndex == sLength)
                    {
                        break;
                    }
                    sChar = s[sIndex];
                }
                int tLeadingZeroCount = 0;
                while (tChar == '0')
                {
                    ++tLeadingZeroCount;
                    ++tIndex;
                    if (tIndex == tLength)
                    {
                        break;
                    }
                    tChar = t[tIndex];
                }
                bool sAllZero = sIndex == sLength || !Char.IsDigit(sChar);
                bool tAllZero = tIndex == tLength || !Char.IsDigit(tChar);
                if (sAllZero && tAllZero)
                {
                    continue;
                }
                if (sAllZero && !tAllZero)
                {
                    return -1;
                }
                if (tAllZero)
                {
                    return 1;
                }

                int diff = 0;
                do
                {
                    if (diff == 0)
                    {
                        diff = sChar - tChar;
                    }
                    ++sIndex;
                    ++tIndex;
                    if (sIndex == sLength && tIndex == tLength)
                    {
                        return diff != 0 ? diff : sLeadingZeroCount - tLeadingZeroCount;
                    }
                    if (sIndex == sLength)
                    {
                        if (diff == 0)
                        {
                            return -1;
                        }
                        return Char.IsDigit(t[tIndex]) ? -1 : diff;
                    }
                    if (tIndex == tLength)
                    {
                        if (diff == 0)
                        {
                            return 1;
                        }
                        return Char.IsDigit(s[sIndex]) ? 1 : diff;
                    }
                    sChar = s[sIndex];
                    tChar = t[tIndex];
                    sCharIsDigit = Char.IsDigit(sChar);
                    tCharIsDigit = Char.IsDigit(tChar);
                    if (!sCharIsDigit && !tCharIsDigit)
                    {
                        if (diff != 0)
                        {
                            return diff;
                        }
                        break;
                    }
                    if (!sCharIsDigit)
                    {
                        return -1;
                    }
                    if (!tCharIsDigit)
                    {
                        return 1;
                    }
                } while (true);
            }
            else
            {
                do
                {
                    if (sChar != tChar)
                    {
                        if (caseSensitive)
                        {
                            return sChar - tChar;
                        }
                        sChar = Char.ToUpper(sChar);
                        tChar = Char.ToUpper(tChar);
                        if (sChar != tChar)
                        {
                            sChar = Char.ToLower(sChar);
                            tChar = Char.ToLower(tChar);
                            if (sChar != tChar)
                            {
                                return sChar - tChar;
                            }
                        }
                    }
                    ++sIndex;
                    ++tIndex;
                    if (sIndex == sLength && tIndex == tLength)
                    {
                        return 0;
                    }
                    if (sIndex == sLength)
                    {
                        return -1;
                    }
                    if (tIndex == tLength)
                    {
                        return 1;
                    }
                    sChar = s[sIndex];
                    tChar = t[tIndex];
                    sCharIsDigit = Char.IsDigit(sChar);
                    tCharIsDigit = Char.IsDigit(tChar);
                } while (!sCharIsDigit && !tCharIsDigit);
            }
        }
    }
}

//Noddy class to represent a sortable row
class SortWrapper
{
    public string sortString;
    public int id;
    public int order;

    public SortWrapper(string sortString, int id, int order)
    {
        this.sortString = sortString;
        this.id = id;
        this.order = order;
    }

    public SortWrapper() { }
}
