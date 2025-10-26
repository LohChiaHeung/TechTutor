using System;

[Serializable]
public class ChatQAPair
{
    public string id;              // GUID
    public string question;        // titleText
    public string answer;          // summaryText (preview in list)
    public long createdAtUnix;     // timeText
    public string imagePath;       // optional (null/empty = no image)

    public ChatQAPair() { }

    public ChatQAPair(string q, string a, string imgPath = null)
    {
        id = Guid.NewGuid().ToString();
        question = q ?? string.Empty;
        answer = a ?? string.Empty;
        createdAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        imagePath = imgPath;
    }
}
