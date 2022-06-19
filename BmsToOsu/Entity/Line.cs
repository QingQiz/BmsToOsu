namespace BmsToOsu.Entity;

public class Line
{
    public string Channel = "";
    public string Message { get; set; } = "";

    public int GetLaneNumber()
    {
        return Channel switch
        {
            "11" => 1,
            "12" => 2,
            "13" => 3,
            "14" => 4,
            "15" => 5,
            "18" => 6,
            "19" => 7,

            "16" => 0,
            "56" => 0,

            "51" => 1,
            "52" => 2,
            "53" => 3,
            "54" => 4,
            "55" => 5,
            "58" => 6,
            "59" => 7,
            _    => -1
        };
    }
}