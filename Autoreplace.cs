//  
//  Autoreplace.cs
//  
//  Author:
//       Francisco José Rodríguez Bogado <bogado@qinn.es>
// 
//  Copyright (c) 2012 Francisco José Rodríguez Bogado
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Text.RegularExpressions;
using Gtk;
using Tomboy;
using Mono.Unix;
using System.Collections.Generic;

/*
 * TODO: Podría tener un cuadro de configuración para especificar más 
 * palabras que reemplazar por otras.
 */

namespace Tomboy.Autoreplace
{
    public class AutoreplaceAddin:NoteAddin
    {
        static string[] PATTERNS = {"<->", "-->", "<--", "---"};
        static string[] REPLACERS = {"↔", "→", "←", "HorizontalLine()"};
        // This is a little tricky. If "replacer" is a string containing "()", then 
        // is not actually a string, but a funcition name to call when "pattern" found.
        
        public override void Initialize(){}
        
        public override void Shutdown(){}
        
        public override void OnNoteOpened(){
            MakeSubs(); // Text inserted with plugin deactivated is now updated.
            Buffer.InsertText += OnInsertText;
        }
        
        public void MakeSubs(){
            NoteBuffer buffer = Note.Buffer;
            int i = 0;
            
            foreach (string s in PATTERNS){ 
                Reemplazar(buffer, s, REPLACERS[i]);
                i++;
            }
        }

        private void OnInsertText(object sender, Gtk.InsertTextArgs args){
            MakeSubs();
        }
        
        private void Reemplazar(NoteBuffer b, string s, string r){
            /*
             * Changes every occurrences of string s for r in buffer b.
             * Why three passess? Because TextIter left invalidated every time I modify
             * content of a note. I use marks to postprocessing deletions and insertions.
             * Despite that, still invalidation warnings happens.
             */
            bool found;
            Gtk.TextIter w;     // Where to insert "r".
            List<Gtk.TextMark> inserciones = new List<Gtk.TextMark>();
                                // Marks to insert.
            Gtk.TextIter ss;    // Start of slice containing the occurrence
            Gtk.TextIter es;    // End of slice
            List<Gtk.TextMark> comienzos = new List<Gtk.TextMark>();
            List<Gtk.TextMark> finales = new List<Gtk.TextMark>();
            int i = 0;
            int j;
            Gtk.TextIter pos;

            // First pass. Finding...
            pos = b.StartIter;
            do{
                found = pos.ForwardSearch(s, Gtk.TextSearchFlags.TextOnly,
                                          out ss, out es, b.EndIter);
                if (found){
                    inserciones.Add(b.CreateMark("check" + i.ToString(), ss, false));
                    comienzos.Add(b.CreateMark("comienzo" + i.ToString(), ss, false));
                    finales.Add(b.CreateMark("final" + i.ToString(), es, false));
                    i++;
                    pos = es;   // Search is started after «s» in next iteration
                }
            }while (found);
            // Second pass. Removing...
            for (j = 0; j < i; j++){
                ss = b.GetIterAtMark(comienzos[j]);
                es = b.GetIterAtMark(finales[j]);
                b.Delete(ref ss, ref es);
            }
            // Third pass. Inserting...
            for (j = 0; j < i; j++){
                w = b.GetIterAtMark(inserciones[j]);
                // Special case. If "Something()" in r, is a method, not a string.
                string methodName = esMetodo(r);
                if (methodName != null)
                    runMethod(methodName, w);
                else
                    b.Insert(ref w, r);
            }
        }
        
        private string esMetodo(string r){
            /* 
             * If «r» is "Something()", return "Something()". Else return null.
             */
            string res = null; 
            
            Regex rx = new Regex(@"[A-Za-z]+\(\)");
            Logger.Info(r);
            Logger.Info("isMatch -> " + rx.IsMatch(r));
            if (rx.IsMatch(r))
                res = r;
            return res;
        }
        
        private void runMethod(string m, Gtk.TextIter i){
            // Don't know if there is an "eval" function like in Python.
            // Don't know how to use pointer to functions like in C, neither.
            // FIXME: HARCODED
            if (m == "HorizontalLine()")
                HorizontalLine(i);
        }
        
        private void HorizontalLine(Gtk.TextIter i){
            /*
             * Insert an horizontal line (a sort of) centered on mark «w».
             */
            Gtk.TextIter ogt = i;
            ogt.BackwardChar();
            // Buffer.AddNewline(false); <-- Didn't work.
            if (! i.StartsLine())
                Buffer.Insert(ref i, "\n");
            Buffer.Insert(ref i, "                ―――――――――――o―――――――――――");
            Buffer.InsertInteractiveAtCursor("\n", true);
        }
    }
}

