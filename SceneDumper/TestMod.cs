using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LSWTSS.OMP.Game.Api;
using System.Text.Json;
using NodeDumper;

namespace LSWTSS.OMP;

public class TestMod : IDisposable
{
    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int vKey);

    private const string componentsPath = @"D:\lswtss-omp-export-latest\apiComponentsDumped.txt";

    private string[] componentNames;

    private Node rootNode;

    private nttUniverse.Handle nttUniverse;

    private PlayerControlSystem.Handle playerControlSystemHandle;

    NativeFuncHook<DoubleJumpContext.OnEnterMethod.Delegate>? doubleJumpContextOnEnterMethodHook;

    void DoCoolStuff(DoubleJumpContext.Handle handle)
    {
        var apiEntityHandle = handle.GetParent();
        if (apiEntityHandle == nint.Zero)
        {
            throw new Exception("Cannot get apiEntity");
        }

        nttUniverse = apiEntityHandle.GetUniverse();

        if (nttUniverse == nint.Zero)
        {
            throw new Exception("Cannot get nttUniverse");
        }

        playerControlSystemHandle = PlayerControlSystem.GetFromGlobalFunc.Execute(nttUniverse);

        if (playerControlSystemHandle == nint.Zero)
        {
            throw new Exception("Cannot get PlayerControlSystem");
        }

        apiEntity.Handle localPlayer = playerControlSystemHandle.GetPlayerEntityForPlayerIdx(0);

        if (localPlayer == nint.Zero)
        {
            throw new Exception("Cannot get LocalPlayer");
        }

        ApiWorld.Handle apiWorld = localPlayer.GetWorld();

        if (apiWorld == nint.Zero)
        {
            throw new Exception("Cannot get ApiWorld");
        }

        rootNode = new Node(apiWorld.GetSceneGraphRoot());

        if (rootNode.Entity == nint.Zero)
        {
            throw new Exception("Cannot get RootNode");
        }

        /*apiEntity.Handle parent = localPlayer.GetParent();

        if (parent == nint.Zero)
        {
            throw new Exception("Cannot get PartyMembers");
        }

        rootNode = new Node(parent.GetParent());

        if (rootNode.Entity == nint.Zero)
        {
            throw new Exception("Cannot get RootNode");
        }*/



        rootNode.RecurseChildren(componentNames);

        File.WriteAllText(@"nodegraph.json", JsonSerializer.Serialize(rootNode));
    }

    public TestMod()
    {
        componentNames = File.ReadAllLines(componentsPath);

        doubleJumpContextOnEnterMethodHook = new(
            DoubleJumpContext.OnEnterMethod.Ptr,
            (DoubleJumpContext.Handle handle, ArgsEnter.Handle param0) =>
            {
                try
                {
                    DoCoolStuff(handle);
                }
                catch (Exception e)
                {
                    Console.WriteLine("EXCEPTION: " + e);
                }

                doubleJumpContextOnEnterMethodHook!.GetTrampoline()(handle, param0);
            }
        );

        doubleJumpContextOnEnterMethodHook.Enable();

        /*nttUniverse = GetUniverseGlobalFunc.Execute();

        if (nttUniverse == nint.Zero)
        {
            throw new Exception("Cannot get nttUniverse");
        }*/


    }

    public void OnUpdate()
    {

    }

    public void Dispose()
    {
        if (doubleJumpContextOnEnterMethodHook != null)
        {
            doubleJumpContextOnEnterMethodHook.Dispose();
            doubleJumpContextOnEnterMethodHook = null;
        }
    }

    ~TestMod()
    {
        Dispose();
    }
}
