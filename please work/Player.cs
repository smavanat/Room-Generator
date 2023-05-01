using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace please_work
{
    class Player
    {
        public Texture2D sprite;
        public Vector2 position;
        public Vector2 targetPosition;

        private Vector2 direction;
        private MouseState oldMouseState;
        private int speed = 50;

        public Player(Texture2D _sprite, Vector2 _position)
        {
            sprite = _sprite;
            position = _position;
            targetPosition = _position;
        }

        public void playerUpdate(GameTime gameTime)
        {
            MouseState mouseState = Mouse.GetState();

            if (mouseState.LeftButton == ButtonState.Pressed)
                targetPosition = new Vector2(mouseState.X, mouseState.Y);

            if(Vector2.Distance(position, targetPosition) > 1)
            {
                direction = Vector2.Normalize(targetPosition - position);

                position += direction * (float)gameTime.ElapsedGameTime.TotalSeconds * speed;
            }
            else
            {
                direction = Vector2.Zero;
            }  
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            int width = sprite.Width;
            int height = sprite.Height;

            Rectangle sourceRectangle = new Rectangle(width, height, width, height); //Image within the texture we want to draw
            Rectangle destinationRectangle = new Rectangle((int)position.X, (int)position.Y, width, height); //Where we want to draw the texture within the game

            spriteBatch.Begin(sortMode: SpriteSortMode.FrontToBack);

            spriteBatch.Draw(sprite, position, Color.White);
            spriteBatch.End();
        }
    }
}
