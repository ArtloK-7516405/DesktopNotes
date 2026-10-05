using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DesktopNotes
{
    /// <summary>Поле с фиксированной маской: "0" в маске — цифра, остальное — неизменные символы.
    /// Удалённая цифра заменяется на 0, разделители удалить нельзя.</summary>
    public static class Mask
    {
        public static void Attach(TextBox tb, string mask)
        {
            if (tb.Text.Length != mask.Length) tb.Text = mask;
            tb.MaxLength = mask.Length;
            tb.IsUndoEnabled = false;
            tb.ContextMenu = null;

            int Next(int i) { while (i < mask.Length && mask[i] != '0') i++; return i; }
            int Prev(int i) { i--; while (i >= 0 && mask[i] != '0') i--; return i; }
            void Put(int slot, char c) { var a = tb.Text.ToCharArray(); a[slot] = c; tb.Text = new string(a); }

            tb.PreviewTextInput += (s, e) =>
            {
                e.Handled = true;
                int pos = tb.SelectionStart;
                foreach (var c in e.Text)
                {
                    if (!char.IsDigit(c)) continue;
                    pos = Next(pos);
                    if (pos >= mask.Length) break;
                    Put(pos, c); pos++;
                }
                tb.CaretIndex = Math.Min(pos, mask.Length);
            };

            tb.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Space || (e.Key == Key.X && Keyboard.Modifiers == ModifierKeys.Control)) { e.Handled = true; return; }
                if (e.Key != Key.Back && e.Key != Key.Delete) return;
                e.Handled = true;
                int st = tb.SelectionStart, len = tb.SelectionLength;
                if (len > 0)
                {
                    var a = tb.Text.ToCharArray();
                    for (int i = st; i < st + len && i < mask.Length; i++) if (mask[i] == '0') a[i] = '0';
                    tb.Text = new string(a); tb.CaretIndex = st; return;
                }
                if (e.Key == Key.Back) { int p = Prev(st); if (p >= 0) { Put(p, '0'); tb.CaretIndex = p; } }
                else { int p = Next(st); if (p < mask.Length) { Put(p, '0'); tb.CaretIndex = p + 1; } }
            };

            DataObject.AddPastingHandler(tb, (s, e) =>
            {
                e.CancelCommand();
                if (e.DataObject.GetData(DataFormats.UnicodeText) is string t)
                {
                    int pos = tb.SelectionStart;
                    foreach (var c in t)
                    {
                        if (!char.IsDigit(c)) continue;
                        pos = Next(pos);
                        if (pos >= mask.Length) break;
                        Put(pos, c); pos++;
                    }
                    tb.CaretIndex = Math.Min(pos, mask.Length);
                }
            });
        }
    }
}
