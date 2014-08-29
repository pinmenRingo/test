﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFTest01.Tetris
{
    class MatchTemplate
    {
        const int MATCH_TEMPLATE_NUM = 19;

        public int[, ,] matchTemplate = {
                                           //T型
                                           {
                                               
                                               {0,0,0,0},
                                               {0,0,3,0},
                                               {0,7,13,11},
                                               {0,0,0,0}
                                           },

                                           {
                                               {0,0,0,0},
                                               {0,0,3,0},
                                               {0,0,7,11},
                                               {0,0,13,0}
                                           },

                                           {
                                               {0,0,0,0},
                                               {0,0,3,0},
                                               {0,7,11,0},
                                               {0,0,13,0}
                                           },

                                           {
                                               {0,0,0,0},
                                               {0,0,0,0},
                                               {0,7,3,11},
                                               {0,0,13,0}
                                           },

                                           //J
                                           {
                                               {0,0,0,0},
                                               {0,3,0,0},
                                               {0,7,13,11},
                                               {0,0,0,0}
                                           },
                                           {
                                               {0,0,0,0},
                                               {0,3,11,0},
                                               {0,7,0,0},
                                               {0,13,0,0}
                                           },
                                           {
                                               {0,0,0,0},
                                               {0,0,0,0},
                                               {7,3,11,0},
                                               {0,0,17,0}
                                           },
                                           {
                                               {0,0,0,0},
                                               {0,7,0,0},
                                               {0,3,0,0},
                                               {13,11,0,0}
                                           },

                                           //L
                                           {
                                               {0,0,0,0},
                                               {0,11,0,0},
                                               {0,3,0,0},
                                               {0,7,17,0},
                                           },
                                           {
                                               {0,0,0,0},
                                               {0,0,0,0},
                                               {0,7,3,11},
                                               {0,13,0,0}
                                           },
                                           {
                                               {0,0,0,0},
                                               {0,3,11,0},
                                               {0,0,7,0},
                                               {0,0,13,0}
                                           },
                                           {
                                               {0,0,0,0},
                                               {0,0,3,0},
                                               {7,13,11,0},
                                               {0,0,0,0 }
                                           },

                                           //S
                                           {
                                               {0,0,0,0},
                                               {0,0,3,11},
                                               {0,7,17,0},
                                               {0,0,0,0}
                                           },

                                           {
                                               {0,0,0,0},
                                               {0,3,0,0},
                                               {0,7,11,0},
                                               {0,0,17,0}
                                           },

                                           //逆S
                                           {
                                               {0,0,0,0},
                                               {0,7,3,0},
                                               {0,0,17,11},
                                               {0,0,0,0}
                                           },

                                           {
                                               {0,0,0,0},
                                               {0,0,11,0},
                                               {0,7,3,0},
                                               {0,13,0,0}
                                           },

                                           // l
                                           {
                                               {0,0,11,0},
                                               {0,0,3,0},
                                               {0,0,7,0},
                                               {0,0,17,0}
                                           },

                                           {
                                               {0,0,0,0},
                                               {0,0,0,0},
                                               {7,3,17,11},
                                               {0,0,0,0}
                                           },

                                           //□
                                           {
                                               {0,0,0,0},
                                               {0,0,0,0},
                                               {0,7,3,0,},
                                               {0,13,11,0}
                                           }

                                       };
        public MatchTemplate()
        {
         
        }

        

    }
}