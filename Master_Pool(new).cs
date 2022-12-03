using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Master_Pool
{
    /*暂时都缺少严谨的容量设置*/

    /*VoidBlockObjects对象池，单一种类*/
    const int maxCapacity_voidBlockObjects = 512;

    static GameObject[] voidBlockObjects;
    static Transform containerTransform_voidBlockObjects;
    static int stockCount_voidBlockobjects;

    /*ExNBricks对象池*/
    static int typeCount_exnBricks;
    const int maxCapacity_exnBricks = 256;//后续应能让每种exnBrick的对象池容量独立设置

    static GameObject[][] exnBricks;
    static Transform containerTransform_exnBricks;
    static int[] stockCount_exnBricks;

    public static void InstantiatePool_VoidBlockObjects()
    {
        containerTransform_voidBlockObjects = new GameObject("Container_VoidBlockObjects").transform;//默认位置为Vector3.zero
        voidBlockObjects = new GameObject[maxCapacity_voidBlockObjects];
        for (int i = 0; i < maxCapacity_voidBlockObjects; i += 1)
        {
            GameObject voidBlockObject = GameObject.Instantiate(Prefabs.voidBlockObject);
            voidBlockObject.SetActive(false);
            voidBlockObject.transform.parent = containerTransform_voidBlockObjects;
            voidBlockObjects[i] = voidBlockObject;
        }
        stockCount_voidBlockobjects = maxCapacity_voidBlockObjects;
    }

    public static void InstantiatePools_ExNBricks()
    {
        typeCount_exnBricks = Prefabs.exnBricks.Count;//由Master_Core保障Prefabs.exnBricks率先被赋值
        containerTransform_exnBricks = new GameObject("Container_ExnBricks").transform;//默认位置为Vector3.zero
        exnBricks = new GameObject[typeCount_exnBricks][];
        stockCount_exnBricks = new int[typeCount_exnBricks];
        for (int type = 0; type < typeCount_exnBricks; type += 1)
        {
            exnBricks[type] = new GameObject[maxCapacity_exnBricks];
            for (int i = 0; i < maxCapacity_exnBricks; i += 1)
            {
                GameObject exnBrick = GameObject.Instantiate(Prefabs.exnBricks[type]);
                exnBrick.SetActive(false);
                exnBrick.transform.parent = containerTransform_exnBricks;
                exnBricks[type][i] = exnBrick;
            }
            stockCount_exnBricks[type] = maxCapacity_exnBricks;
        }
    }

    public static GameObject TryReceiveVoidBlockObject()
    {
        if (stockCount_voidBlockobjects > 0)
        {
            stockCount_voidBlockobjects -= 1;
            GameObject thisVoidBlockObject = voidBlockObjects[stockCount_voidBlockobjects];
            thisVoidBlockObject.SetActive(true);
            return thisVoidBlockObject;
        }
        else
        {
            GameObject newVoidBlockObject = GameObject.Instantiate(Prefabs.voidBlockObject);
            newVoidBlockObject.transform.parent = containerTransform_voidBlockObjects;
            return newVoidBlockObject;
        }
    }

    public static void TryRecycleVoidBlockObject(GameObject thisVoidBlockObject)
    {
        if (stockCount_voidBlockobjects < maxCapacity_voidBlockObjects)
        {
            thisVoidBlockObject.SetActive(false);
            voidBlockObjects[stockCount_voidBlockobjects] = thisVoidBlockObject;
            stockCount_voidBlockobjects += 1;
        }
        else
        {
            GameObject.Destroy(thisVoidBlockObject);
        }
    }

    public static GameObject TryReceiveExNBrick(int type)
    {
        if (stockCount_exnBricks[type] > 0)
        {
            stockCount_exnBricks[type] -= 1;
            GameObject thisExNBrick = exnBricks[type][stockCount_exnBricks[type]];
            thisExNBrick.SetActive(true);
            return thisExNBrick;
        }
        else
        {
            GameObject newExNBrick = GameObject.Instantiate(Prefabs.exnBricks[type]);
            newExNBrick.transform.parent = containerTransform_exnBricks;
            return newExNBrick;
        }
    }
    public static void TryRecycleExNBrick(GameObject thisExNBrick, int type)
    {
        if (stockCount_exnBricks[type] < maxCapacity_exnBricks)
        {
            thisExNBrick.SetActive(false);
            exnBricks[type][stockCount_exnBricks[type]] = thisExNBrick;
            stockCount_exnBricks[type] += 1;
        }
        else
        {
            GameObject.Destroy(thisExNBrick);
        }
    }
}
