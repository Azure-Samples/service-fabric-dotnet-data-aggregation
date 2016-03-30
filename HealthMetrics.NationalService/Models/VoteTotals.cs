using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Election.Common;

namespace Election.NationalService.Models
{
    public struct VoteTotals
    {
        public VoteTotals(IDictionary<ShirtEnum, int> candidateTotals, IDictionary<string, ShirtEnum> countyWinners)
        {
            this.CandidatesTotals = candidateTotals;
            this.CountyWinners = countyWinners;
        }

        public IDictionary<ShirtEnum, int> CandidatesTotals { get; private set; }

        public IDictionary<string, ShirtEnum> CountyWinners { get; private set; }
    }
}
