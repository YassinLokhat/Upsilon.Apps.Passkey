using Microsoft.Maui.Graphics;

namespace Upsilon.Apps.Passkey.GUI.MAUI.Views
{
   /// <summary>
   /// Renders a QR <c>bool[,]</c> matrix without System.Drawing.
   /// </summary>
   public sealed class QrCodeDrawableView : GraphicsView
   {
      public static readonly BindableProperty MatrixProperty = BindableProperty.Create(
         nameof(Matrix),
         typeof(bool[,]),
         typeof(QrCodeDrawableView),
         defaultValue: new bool[0, 0],
         propertyChanged: static (bindable, _, newValue) =>
         {
            if (bindable is QrCodeDrawableView view)
            {
               view._drawable.Matrix = newValue as bool[,] ?? new bool[0, 0];
               view.Invalidate();
            }
         });

      private readonly QrMatrixDrawable _drawable = new();

      public QrCodeDrawableView()
      {
         Drawable = _drawable;
         BackgroundColor = Colors.White;
      }

      public bool[,] Matrix
      {
         get => (bool[,])GetValue(MatrixProperty);
         set => SetValue(MatrixProperty, value);
      }

      private sealed class QrMatrixDrawable : IDrawable
      {
         public bool[,] Matrix { get; set; } = new bool[0, 0];

         public void Draw(ICanvas canvas, RectF dirtyRect)
         {
            int rows = Matrix.GetLength(0);
            int cols = Matrix.GetLength(1);
            if (rows == 0 || cols == 0)
            {
               return;
            }

            canvas.FillColor = Colors.White;
            canvas.FillRectangle(dirtyRect);

            float cell = Math.Min(dirtyRect.Width / (cols + 2), dirtyRect.Height / (rows + 2));
            float offsetX = (dirtyRect.Width - cell * cols) / 2f;
            float offsetY = (dirtyRect.Height - cell * rows) / 2f;

            canvas.FillColor = Colors.Black;
            for (int r = 0; r < rows; r++)
            {
               for (int c = 0; c < cols; c++)
               {
                  if (Matrix[r, c])
                  {
                     canvas.FillRectangle(offsetX + c * cell, offsetY + r * cell, cell, cell);
                  }
               }
            }
         }
      }
   }
}
