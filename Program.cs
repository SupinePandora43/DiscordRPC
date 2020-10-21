using System;
using System.Linq;
using System.Threading;
using System.Text;
using System.Runtime.InteropServices;

class Program
{
    // Request user's avatar data. Sizes can be powers of 2 between 16 and 2048
    static void FetchAvatar(Discord.ImageManager imageManager, Int64 userID)
    {
        imageManager.Fetch(Discord.ImageHandle.User(userID), (result, handle) =>
        {
            {
                if (result == Discord.Result.Ok)
                {
                    // You can also use GetTexture2D within Unity.
                    // These return raw RGBA.
                    var data = imageManager.GetData(handle);
                    /*try
                    {
                        var ms = new MemoryStream(data);
                        Bitmap bitmap = new Bitmap(ms);
                        bitmap.Save(handle.Id + ".bmp");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }*/
                    Console.WriteLine("image updated {0} {1}", handle.Id, data.Length);
                }
                else
                {
                    Console.WriteLine("image error {0}", handle.Id);
                }
            }
        });
    }

    // Update user's activity for your game.
    // Party and secrets are vital.
    // Read https://discordapp.com/developers/docs/rich-presence/how-to for more details.
    

    static void Main(string[] args)
    {
        
    }
}
