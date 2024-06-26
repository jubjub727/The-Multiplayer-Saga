﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using OMP.LSWTSS.Api1;


namespace NodeDumper;
public class Node
{
    [JsonIgnore]
    public static apiEntity.Handle RootNode;

    [JsonIgnore]
    public Node Parent;

    public string Label { get; set; }
    public List<Node> Children { get; set; }

    [JsonIgnore]
    public apiComponent.Handle Component = (apiComponent.Handle)nint.Zero;
    [JsonIgnore]
    public apiEntity.Handle Entity = (apiEntity.Handle)nint.Zero;

    [JsonIgnore]
    public BigInteger Guid;

    [JsonIgnore]
    private int RecursionIterations = 0;

    [JsonIgnore]
    private const int GuidOffset = 0x46;
    [JsonIgnore]
    private const int Sizeof64Int = 0x8;

    [JsonIgnore]
    private const int MaxRecursions = 1000;

    public Node(Node parent)
    {
        Children = new List<Node>();
        Parent = parent;
    }

    public Node(apiEntity.Handle entity)
    {
        Children = new List<Node>();
        Entity = entity;
        Component = (apiComponent.Handle)(nint)entity;
        RootNode = entity;
        Label = Entity.GetLabel();
    }

    private bool ComponentIsEntity(apiComponent.Handle component)
    {
        var apiEntityVtablePtr = Marshal.ReadIntPtr(component.Ptr);
        return (apiEntityVtablePtr == NativeFunc.GetPtr(0x41E8B90));
    }

    public bool FindChildComponent(string[] componentNames)
    {
        foreach (string componentName in componentNames)
        {
            if (IsEntity())
            {
                apiComponent.Handle component = Entity.FindComponentByTypeName(componentName);

                if (component != nint.Zero)
                {
                    Node child = CreateChild(component);
                    Children.Add(child);
                    return true;
                }
            }
        }
        return false;
    }

    private static BigInteger GetGuid(apiComponent.Handle component)
    {
        var guidPart1 = Marshal.ReadInt64(component.Ptr + GuidOffset);
        var guidPart2 = Marshal.ReadInt64(component.Ptr + GuidOffset + Sizeof64Int);

        return new BigInteger(guidPart2) << 64 | new BigInteger(guidPart1);
    }

    private Node? FindChild(BigInteger guid)
    {
        foreach (Node child in Children)
        {
            if (child.Guid == guid)
            {
                return child;
            }
        }
        return null;
    }

    private Node CreateChild(apiComponent.Handle component)
    {
        Node child = new Node(this);

        if (ComponentIsEntity(component))
        {
            child.Entity = (apiEntity.Handle)(nint)component;
            child.Component = component;
        }
        else
        {
            child.Component = component;
        }

        child.Label = child.Component.GetLabel();

        child.Guid = GetGuid(child.Component);

        return child;
    }

    // This code is important because here be dragons of the stackoverflow kind
    private bool IsHeadingTowardsStackOverflow() 
    {
        RecursionIterations++;
        if (RecursionIterations > MaxRecursions)
        {
            return true;
        }

        return false;
    }

    private void IterateForwards(Node node)
    {
        if (IsHeadingTowardsStackOverflow())
        {
            return; // Let's dip from this party
        }

        apiComponent.Handle nextComponent = node.Component.NextSibling();
        if (nextComponent != nint.Zero)
        {
            Node? child = FindChild(GetGuid(nextComponent));

            if (child == null)
            {
                child = CreateChild(nextComponent);
                Children.Add(child);
            }

            IterateForwards(child);
        }
    }

    private void IterateBackwards(Node node)
    {
        if (IsHeadingTowardsStackOverflow())
        {
            return; // Let's dip from this party
        }

        apiComponent.Handle prevComponent = node.Component.PrevSibling();
        if (prevComponent != nint.Zero)
        {
            Node? child = FindChild(GetGuid(prevComponent));

            if (child == null)
            {
                child = CreateChild(prevComponent);
                Children.Add(child);
            }

            IterateBackwards(child);
        }
    }

    public void FindAllSiblings()
    {
        RecursionIterations = 0;
        IterateForwards(this);

        RecursionIterations = 0;
        IterateBackwards(this);
    }

    public void FindAllChildren()
    {
        if (Children.Count < 1)
        {
            return;
        }

        RecursionIterations = 0;
        IterateForwards((Node)Children[0]);

        RecursionIterations = 0;
        IterateBackwards((Node)Children[0]);
    }

    public bool IsEntity()
    {
        return Entity != nint.Zero;
    }

    public void RecurseChildren(string[] componentNames)
    {
        if (IsEntity())
        {
            if (FindChildComponent(componentNames))
            {
                FindAllChildren();

                foreach (Node childNode in Children)
                {
                    childNode.RecurseChildren(componentNames);
                }
            }
        }
    }
}
