/*  
*  This Source Code Form is subject to the terms of the Mozilla Public
*  License, v. 2.0. If a copy of the MPL was not distributed with this
*  file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
using System;
using System.Diagnostics;

namespace KarrotSoundProduction;

public class Utils
{
    public static bool CheckForCommand(string command)
    {
        Process whichProcess = new();
        whichProcess.StartInfo = new("which", command)
        {
            RedirectStandardOutput = true
        };

        whichProcess.Start();
        whichProcess.WaitForExit();
        string whichOutput = whichProcess.StandardOutput.ReadToEnd();

        return whichOutput.StartsWith('/');
    }
}