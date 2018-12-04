using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server {
    public class EntryResponse {
        private bool _sucess;
        private int _term;
        private int _matchIndex;

        public bool Sucess { get => _sucess; set => _sucess = value; }
        public int Term { get => _term; set => _term = value; }
        public int MatchIndex { get => _matchIndex; set => _matchIndex = value; }

        public EntryResponse(bool sucess, int term, int matchIndex) {
            _sucess = sucess;
            _term = term;
            _matchIndex = matchIndex;
        }


    }
}
