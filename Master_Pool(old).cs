using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Master_Pool : MonoBehaviour
{

    /*暂时都缺少严谨的容量设置*/

    /*ExNBricks缓存池，多种exnBricks*/
    public static List<List<GameObject>> exnBricks;
    public static GameObject container_exnBricks;
    //对象池数量应维持在128-256之间，以免动态容量变化导致性能问题（？）
    const int numMax_exnBricks = 256;
    const int numMin_exnBricks = 160;
    const int numSupplementPerFrame_exnBricks = 4;//非紧急状态下每帧补充=4

    List<bool> isOptimizing_exnBricks;

    /*VoidBlockObjects缓存池，单一种类*/
    public static List<GameObject> voidBlockObjects;
    public static GameObject container_voidBlockObjects;
    //对象池数量应维持在256-512之间，以免动态容量变化导致性能问题（？）
    const int numMax_voidBlockObjects = 512;
    const int numMin_voidBlockObjects = 280;
    const int numSupplementPerFrame_voidBlockObjects = 8;//非紧急状态下每帧补充=8

    bool isOptimizing_voidBlockObjects;

    void Update()
    {
        if (!isOptimizing_voidBlockObjects)
            OptimizePool_VoidBlockObjects();

        for (int i = 0; i < isOptimizing_exnBricks.Count; i += 1)
        {
            if (!isOptimizing_exnBricks[i])
                OptimizePool_ExNBricks(i);
        }
    }

    public void InstantiatePool_VoidBlockObjects()
    {
        container_voidBlockObjects = new("Container_VoidBlockObjects");//默认位置为Vector3.zero
        voidBlockObjects = new();
        isOptimizing_voidBlockObjects = false;
        for (int i = 0; i < numMax_voidBlockObjects; i += 1)
        {
            GameObject voidBlockObject = GameObject.Instantiate(Prefabs.voidBlockObject);
            voidBlockObject.SetActive(false);
            voidBlockObject.transform.parent = container_voidBlockObjects.transform;
            voidBlockObjects.Add(voidBlockObject);
        }
    }

    public void InstantiatePools_ExNBricks()
    {
        container_exnBricks = new("Container_ExnBricks");//默认位置为Vector3.zero
        exnBricks = new();
        isOptimizing_exnBricks = new();
        for (int i = 0; i < Prefabs.exnBricks.Count; i += 1)
        {
            exnBricks.Add(new List<GameObject>());
            isOptimizing_exnBricks.Add(false);
            for (int n = 0; n < numMax_exnBricks; n += 1)
            {
                GameObject exnBrick = GameObject.Instantiate(Prefabs.exnBricks[i]);
                exnBrick.SetActive(false);
                exnBrick.transform.parent = container_exnBricks.transform;
                exnBricks[i].Add(exnBrick);
            }
        }
    }

    void OptimizePool_VoidBlockObjects()
    {
        if (voidBlockObjects.Count < numMin_voidBlockObjects)
        {
            isOptimizing_voidBlockObjects = true;
            StartCoroutine(VoidBlockObjectsPool_AddToModerateNum());
        }
    }
    IEnumerator VoidBlockObjectsPool_AddToModerateNum()//每帧numSupplementPerFrame_voidBlockObjects个补充到数量不低于numMin_voidBlockObjects
    {
        while (voidBlockObjects.Count < numMin_voidBlockObjects)
        {
            for (int i = 0; i < numSupplementPerFrame_voidBlockObjects; i += 1)
            {
                GameObject voidBlockObject = Instantiate(Prefabs.voidBlockObject);
                voidBlockObjects.Add(voidBlockObject);
                voidBlockObject.transform.parent = container_voidBlockObjects.transform;
                voidBlockObject.SetActive(false);
            }
            yield return null;
        }
        isOptimizing_voidBlockObjects = false;
    }

    public void OptimizePool_ExNBricks(int n)
    {
        if (exnBricks[n].Count < numMin_exnBricks)
        {
            isOptimizing_exnBricks[n] = true;
            StartCoroutine(ExNBricksPool_AddToModerateNum(n));
        }
    }

    IEnumerator ExNBricksPool_AddToModerateNum(int n)//每帧numSupplementPerFrame_exnBricks个补充到数量不低于numMin_exnBricks
    {
        while (exnBricks[n].Count < numMin_exnBricks)
        {
            for (int i = 0; i < numSupplementPerFrame_exnBricks; i += 1)
            {
                GameObject exnBrick = Instantiate(Prefabs.exnBricks[n]);
                exnBricks[n].Add(exnBrick);
                exnBrick.transform.parent = container_exnBricks.transform;
                exnBrick.SetActive(false);
            }
            yield return null;
        }
        isOptimizing_exnBricks[n] = false;
    }

    public static void TryRecycleExNBrick(GameObject thisExnBrick, int type)
    {
        if (exnBricks[type].Count < numMax_exnBricks)
        {
            thisExnBrick.SetActive(false);
            exnBricks[type].Add(thisExnBrick);
        }
        else
        {
            Destroy(thisExnBrick);
        }
    }

    public static void TryRecycleVoidBlockObject(GameObject thisVoidBlockObject)
    {
        if (voidBlockObjects.Count < numMax_voidBlockObjects)
        {
            thisVoidBlockObject.SetActive(false);
            voidBlockObjects.Add(thisVoidBlockObject);
        }
        else
        {
            Destroy(thisVoidBlockObject);
        }
    }
}
