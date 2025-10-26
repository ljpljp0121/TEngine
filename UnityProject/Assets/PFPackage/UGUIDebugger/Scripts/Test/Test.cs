using UnityEngine;

namespace PFDebugger
{
    [DebuggerWindow("Test3/TestWindow1")]
    public class TestWindow1 : IWindowBase
    {
        
    }
    
    [DebuggerWindow("Test3/TestWindow2")]
    public class TestWindow2 : IWindowBase
    {
        
    }
    
    [DebuggerWindow("Test1/TestWindow3")]
    public class TestWindow3 : IWindowBase
    {
        
    }
    
    [DebuggerWindow("Test2/TestWindow4")]
    public class TestWindow4 : IWindowBase
    {
        
    }
    
    public class Test
    {
     
        [DebuggerWindow("Test1/MethodTest")]
        public static void MethodTest()
        { 
            Debug.Log("MethodTest");
        }
    }
}