using UnityEngine;

namespace PFDebugger
{
    [DebuggerWindow("Test3/TestWindow1")]
    public class TestWindow1 : WindowBase
    {
        
    }
    
    [DebuggerWindow("Test3/TestWindow2")]
    public class TestWindow2 : WindowBase
    {
        
    }
    
    [DebuggerWindow("Test1/TestWindow3")]
    public class TestWindow3 : WindowBase
    {
        
    }
    
    [DebuggerWindow("Test2/TestWindow4")]
    public class TestWindow4 : WindowBase
    {
        
    }
    
    public class Test
    {
     
        [DebuggerWindow("Close")]
        public static void MethodTest()
        { 
            DebuggerManager.I.ShowMainWindow(false);
        }
    }
}