﻿/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System.Collections.Generic;
using TUIOsharp;
using UnityEngine;

namespace TouchScript.InputSources
{
    /// <summary>
    /// Processes TUIO 1.0 input.
    /// </summary>
    [AddComponentMenu("TouchScript/Input Sources/TUIO Input")]
    public sealed class TuioInput : InputSourcePro
    {
        #region Unity fields

        /// <summary>
        /// Port to listen to.
        /// </summary>
        public int TuioPort = 3333;

        /// <summary>
        /// Minimum movement delta to ignore in cm.
        /// </summary>
        public float MovementThreshold = 0f;

        #endregion

        #region Private variables

        private TuioServer server;
        private Dictionary<TuioCursor, int> cursorToInternalId = new Dictionary<TuioCursor, int>();
        private int screenWidth;
        private int screenHeight;

        #endregion

        #region Unity

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            server = new TuioServer(TuioPort);
            server.MovementThreshold = MovementThreshold * manager.DotsPerCentimeter/Mathf.Max(Screen.width, Screen.height);
            server.CursorAdded += OnCursorAdded;
            server.CursorUpdated += OnCursorUpdated;
            server.CursorRemoved += OnCursorRemoved;
            server.Connect();
        }

        /// <inheritdoc />
        protected override void Update()
        {
            base.Update();
            screenWidth = Screen.width;
            screenHeight = Screen.height;
        }

        /// <inheritdoc />
        protected override void OnDisable()
        {
            if (server != null)
            {
                server.CursorAdded -= OnCursorAdded;
                server.CursorUpdated -= OnCursorUpdated;
                server.CursorRemoved -= OnCursorRemoved;
                server.Disconnect();
            }

            foreach (var i in cursorToInternalId)
            {
                cancelTouch(i.Value);
            }

            base.OnDisable();
        }

        #endregion

        #region Event handlers

        private void OnCursorAdded(object sender, TuioCursorEventArgs tuioCursorEventArgs)
        {
            var cursor = tuioCursorEventArgs.Cursor;
            lock (this)
            {
                var x = cursor.X*screenWidth;
                var y = (1 - cursor.Y)*screenHeight;
                cursorToInternalId.Add(cursor, beginTouch(new Vector2(x, y)));
            }
        }

        private void OnCursorUpdated(object sender, TuioCursorEventArgs tuioCursorEventArgs)
        {
            var cursor = tuioCursorEventArgs.Cursor;
            lock (this)
            {
                int existingCursor;
                if (!cursorToInternalId.TryGetValue(cursor, out existingCursor)) return;

                var x = cursor.X*screenWidth;
                var y = (1 - cursor.Y)*screenHeight;

                moveTouch(existingCursor, new Vector2(x, y));
            }
        }

        private void OnCursorRemoved(object sender, TuioCursorEventArgs tuioCursorEventArgs)
        {
            var cursor = tuioCursorEventArgs.Cursor;
            lock (this)
            {
                int existingCursor;
                if (!cursorToInternalId.TryGetValue(cursor, out existingCursor)) return;

                cursorToInternalId.Remove(cursor);
                endTouch(existingCursor);
            }
        }

        #endregion
    }
}