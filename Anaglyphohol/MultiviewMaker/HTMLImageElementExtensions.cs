using SpawnDev.BlazorJS.JSObjects;

namespace Anaglyphohol.MultiviewMaker
{
    public static class HTMLImageElementExtensions
    {
        /// <summary>
        /// Check if an image is usable by drawing it to an offscreen canvas and trying to read back a pixel<br/>
        /// Images that would cause a tainted canvas will return false
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static bool IsImageUsable(this HTMLImageElement image)
        {
            if (image == null) return false;
            if (!image.Complete || image.NaturalHeight == 0 || image.NaturalWidth == 0) return false;
            try
            {
                using var canvas = new OffscreenCanvas(1, 1);
                using var ctx = canvas.Get2DContext();
                ctx.DrawImage(image, 0, 0);
                using var pixel = ctx.GetImageData(0, 0, 1, 1);
                return true;
            }
            catch (Exception ex)
            {
#if DEBUG && false
                Console.WriteLine("Image is tainted: " + ex.Message);
#endif
                return false;
            }
        }
    }
}
