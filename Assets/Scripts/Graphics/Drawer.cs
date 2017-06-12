using UnityEngine;

namespace GB.Graphics
{
    public class Drawer : MonoBehaviour
    {
        private Texture2D mainTexture;

        private SpriteRenderer sr;

        private Rect rect;

        private Vector2 pivot;

        private long[] prevCanvasBuffer;

        private void Awake()
        {
            mainTexture = new Texture2D(160, 144);
            rect = new Rect(0, 0, mainTexture.width, mainTexture.height);
            pivot = new Vector2(0.5f, 0.5f);
            sr = GetComponent<SpriteRenderer>();
        }

        public void Draw(long[] canvasBuffer)
        {
            if (prevCanvasBuffer != null && AreEquals(prevCanvasBuffer, canvasBuffer))
                return;

            Color black = Color.black;
            Color white = Color.white;

            for (int j = 0; j < 144; j++)
            {
                for (int i = 0; i < 160; i++)
                {
                    int pixelCanvasNumber = i + (160 * j);

                    long pixel = canvasBuffer[pixelCanvasNumber];
                    long prevPixel = prevCanvasBuffer != null ? prevCanvasBuffer[pixelCanvasNumber] : -1;

                    if (pixel != prevPixel)
                    {
                        if (pixel != 0)
                        {
                            long r = (pixel >> 16) & 0xFF;
                            long g = (pixel >> 8) & 0xFF;
                            long b = pixel & 0xFF;
                            mainTexture.SetPixel(i, j, new Color(r / 255f, g / 255f, b / 255f));
                        }
                        else
                            mainTexture.SetPixel(i, j, white);
                    }
                }
            }

            mainTexture.Apply();

            if (sr.sprite == null)
                sr.sprite = Sprite.Create(mainTexture, rect, pivot, 1);

            if (prevCanvasBuffer == null)
                prevCanvasBuffer = new long[canvasBuffer.Length];

            for (int i = 0; i < canvasBuffer.Length; i++)
                prevCanvasBuffer[i] = canvasBuffer[i];
        }

        private bool AreEquals(long[] a, long[] b)
        {
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i])
                    return false;
            return true;
        }
    }
}