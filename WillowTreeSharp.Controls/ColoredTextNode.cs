/*  This file is part of WillowTree#
 * 
 *  Copyright (C) 2011 Matthew Carter <matt911@users.sf.net>
 *  Copyright (C) 2010, 2011 XanderChaos
 *  Copyright (C) 2011 Thomas Kaiser
 *  Copyright (C) 2010 JackSchitt
 * 
 *  WillowTree# is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  WillowTree# is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with WillowTree#.  If not, see <http://www.gnu.org/licenses/>.
 */

using Aga.Controls.Tree;
using System;
using System.Drawing;

namespace WillowTree
{

    public class ColoredTextNode : Node
    {
        /// <exception cref="ArgumentNullException">Argument is null.</exception>
        public override string Text
        {
            get
            {
                return base.Text;
            }
            set
            {
                //if (string.IsNullOrEmpty(value))
                //    throw new ArgumentNullException();

                base.Text = value;
            }
        }

        //public string _Name;
        public string Key
        {
            get { return Tag as string; }
            set { Tag = value; }
        }

        private Color _ForeColor;
        public Color ForeColor
        {
            get { return _ForeColor; }
            set { _ForeColor = value; }
        }

        private Font _Font;
        public Font Font
        {
            get { return _Font; }
            set { _Font = value; }
        }

        public ColoredTextNode()
            : base()
        {
            this._Font = null;
            ForeColor = Color.Black;
        }

        /// <summary>
        /// Initializes a new MyNode class with a given Text property.
        /// </summary>
        /// <param name="text">String to set the text property with.</param>
        public ColoredTextNode(string text)
            : base(text)
        {
            this._Font = null;
            ForeColor = Color.Black;
        }
        public ColoredTextNode(string key, string text)
            : base(text)
        {
            this._Font = null;
            Tag = key;
            ForeColor = Color.Black;
        }

        public void Remove()
        {
            Parent = null;
        }
    }
}

