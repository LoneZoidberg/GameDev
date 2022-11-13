using System.Collections.Generic;
using UnityEngine;

public struct SurfaceScanData
{
    public Vector3 enterPoint;
    /* 
     * 若surfacesToScan==3，两个辅轴从stepsB==1,setpsC==1开始交错扫描，主轴与第一辅助轴从stepsA==0,stepsB==0开始交错扫描，主轴与第二辅助轴从stepsA==0,stepsB==1开始交错扫描
     * 若surfacesToScan==2，主轴与第一辅助轴从stepsA==0,stepsB==0开始交错扫描，主轴与第二辅助轴从stepsA==0,stepsB==1开始交错扫描
     * 若surfacesToScan==1，主轴第一辅助轴从stepsA==0,stepsB==0开始交错扫描
     * 若surfacesToScan==0，直接进入部署
     * 若surfacesToScan==-1, 不进行扫描
     */
    public int surfacesToScan;
    public Vector3 startVertex;
    public Vector3 directionA;//主扫描方向
    public float spacingA;
    public int stepsA;
    public Vector3 directionB;
    public float spacingB;
    public int stepsB;
    public Vector3 directionC;
    public float spacingC;
    public int stepsC;

    public static SurfaceScanData toBeIgnored = new SurfaceScanData
    {
        surfacesToScan = -1,
        startVertex = Vector3.zero,
        directionA = Vector3.zero,
        spacingA = 0f,
        stepsA = 0,
        directionB = Vector3.zero,
        spacingB = 0f,
        stepsB = 0,
        directionC = Vector3.zero,
        spacingC = 0f,
        stepsC = 0
    };
}

public delegate void DelegatedLampMethod();

public class Lamp
{
    /*类的成员变量*/
    public Transform lightTrans;//"__Light"子物体的transform
    public Light lightComponent;

    public List<Block> blocksCoveredByThis_previous;
    public List<Block> blocksCoveredByThis_now;

    public float solidProbability;
    public int brickSource;

    public DelegatedLampMethod UpdateGroundBlockCoverage;

    /*构造函数*/
    public Lamp(GameObject lampObject, float solidProbability, int brickSource)//brickSource==-1，ExNBricksPool;else, prefabRelicBricks[brickSource]
    {
        lightTrans = lampObject.transform.Find("__Light");
        lightComponent = lightTrans.GetComponent<Light>();
        lightComponent.intensity = lightComponent.range * Map.ratio_lightIntensityToRange;

        blocksCoveredByThis_previous = new();
        blocksCoveredByThis_now = new();

        this.solidProbability = solidProbability;
        this.brickSource = brickSource;

        if (lightComponent.type == LightType.Point)
        {
            UpdateGroundBlockCoverage = UpdateGroundBlockCoverage_Sphere;
        }
        else if (lightComponent.type == LightType.Spot)
        {
            UpdateGroundBlockCoverage = UpdateGroundBlockCoverage_Spot;
        }
        else
            Debug.Log("读取光源类型错误！ 光源父物体位置为：" + lampObject.transform.position);

    }

    public SurfaceScanData SurfaceScanData_AsSphereLamp(int column, int row)
    {
        //声明LineCast检测参数，待赋值
        SurfaceScanData surfaceScanData;

        //判断光源相对于block的位置，判断是否在无遮挡条件下被光源照射，若是，设定LineCast检测参数
        if (lightTrans.position.x > (column + 1) * Map.edgeLengthX_block)//X+
        {
            if (lightTrans.position.y > Map.halfEdgeLengthY_block)//X+Y+
            {
                if (lightTrans.position.z > (row + 1) * Map.edgeLengthZ_block)//X+Y+Z+
                {
                    surfaceScanData.enterPoint = new((column + 1) * Map.edgeLengthX_block, Map.halfEdgeLengthY_block, (row + 1) * Map.edgeLengthZ_block);
                    if ((lightTrans.position - surfaceScanData.enterPoint).sqrMagnitude > lightComponent.range * lightComponent.range)
                        return(SurfaceScanData.toBeIgnored);
                    else
                    {
                        surfaceScanData.surfacesToScan = 3;
                        surfaceScanData.startVertex = new((column + 1) * Map.edgeLengthX_block, Map.halfEdgeLengthY_block, (row + 1) * Map.edgeLengthZ_block);
                        surfaceScanData.directionA = Vector3.down;
                        surfaceScanData.spacingA = Map.scanSpacingY_sphere_block;
                        surfaceScanData.stepsA = Map.scanStepsY_sphere_block;
                        surfaceScanData.directionB = Vector3.left;
                        surfaceScanData.spacingB = Map.scanSpacingX_sphere_block;
                        surfaceScanData.stepsB = Map.scanStepsX_sphere_block;
                        surfaceScanData.directionC = Vector3.back;
                        surfaceScanData.spacingC = Map.scanSpacingZ_sphere_block;
                        surfaceScanData.stepsC = Map.scanStepsZ_sphere_block;
                    }
                }
                else if (lightTrans.position.z < row * Map.edgeLengthZ_block)//X+Y+Z-
                {
                    surfaceScanData.enterPoint = new((column + 1) * Map.edgeLengthX_block, Map.halfEdgeLengthY_block, row * Map.edgeLengthZ_block);
                    if ((lightTrans.position - surfaceScanData.enterPoint).sqrMagnitude > lightComponent.range * lightComponent.range)
                        return(SurfaceScanData.toBeIgnored);
                    else
                    {
                        surfaceScanData.surfacesToScan = 3;
                        surfaceScanData.startVertex = new((column + 1) * Map.edgeLengthX_block, Map.halfEdgeLengthY_block, row * Map.edgeLengthZ_block);
                        surfaceScanData.directionA = Vector3.down;
                        surfaceScanData.spacingA = Map.scanSpacingY_sphere_block;
                        surfaceScanData.stepsA = Map.scanStepsY_sphere_block;
                        surfaceScanData.directionB = Vector3.left;
                        surfaceScanData.spacingB = Map.scanSpacingX_sphere_block;
                        surfaceScanData.stepsB = Map.scanStepsX_sphere_block;
                        surfaceScanData.directionC = Vector3.forward;
                        surfaceScanData.spacingC = Map.scanSpacingZ_sphere_block;
                        surfaceScanData.stepsC = Map.scanStepsZ_sphere_block;
                    }
                }
                else//X+Y+Z=
                {
                    surfaceScanData.enterPoint = new((column + 1) * Map.edgeLengthX_block, Map.halfEdgeLengthY_block, lightTrans.position.z);
                    if ((lightTrans.position - surfaceScanData.enterPoint).sqrMagnitude > lightComponent.range * lightComponent.range)
                        return(SurfaceScanData.toBeIgnored);
                    else
                    {
                        surfaceScanData.surfacesToScan = 2;
                        surfaceScanData.startVertex = new((column + 1) * Map.edgeLengthX_block, Map.halfEdgeLengthY_block, row * Map.edgeLengthZ_block);
                        surfaceScanData.directionA = Vector3.forward;
                        surfaceScanData.spacingA = Map.scanSpacingZ_sphere_block;
                        surfaceScanData.stepsA = Map.scanStepsZ_sphere_block;
                        surfaceScanData.directionB = Vector3.left;
                        surfaceScanData.spacingB = Map.scanSpacingX_sphere_block;
                        surfaceScanData.stepsB = Map.scanStepsX_sphere_block;
                        surfaceScanData.directionC = Vector3.down;
                        surfaceScanData.spacingC = Map.scanSpacingY_sphere_block;
                        surfaceScanData.stepsC = Map.scanStepsY_sphere_block;
                    }
                }
            }
            else if (lightTrans.position.y < -Map.halfEdgeLengthY_block)//X+Y-
            {
                if (lightTrans.position.z > (row + 1) * Map.edgeLengthZ_block)//X+Y-Z+
                {
                    surfaceScanData.enterPoint = new((column + 1) * Map.edgeLengthX_block, -Map.halfEdgeLengthY_block, (row + 1) * Map.edgeLengthZ_block);
                    if ((lightTrans.position - surfaceScanData.enterPoint).sqrMagnitude > lightComponent.range * lightComponent.range)
                        return(SurfaceScanData.toBeIgnored);
                    else
                    {
                        surfaceScanData.surfacesToScan = 3;
                        surfaceScanData.startVertex = new((column + 1) * Map.edgeLengthX_block, -Map.halfEdgeLengthY_block, (row + 1) * Map.edgeLengthZ_block);
                        surfaceScanData.directionA = Vector3.up;
                        surfaceScanData.spacingA = Map.scanSpacingY_sphere_block;
                        surfaceScanData.stepsA = Map.scanStepsY_sphere_block;
                        surfaceScanData.directionB = Vector3.left;
                        surfaceScanData.spacingB = Map.scanSpacingX_sphere_block;
                        surfaceScanData.stepsB = Map.scanStepsX_sphere_block;
                        surfaceScanData.directionC = Vector3.back;
                        surfaceScanData.spacingC = Map.scanSpacingZ_sphere_block;
                        surfaceScanData.stepsC = Map.scanStepsZ_sphere_block;
                    }
                }
                else if (lightTrans.position.z < row * Map.edgeLengthZ_block)//X+Y-Z-
                {
                    surfaceScanData.enterPoint = new((column + 1) * Map.edgeLengthX_block, -Map.halfEdgeLengthY_block, row * Map.edgeLengthZ_block);
                    if ((lightTrans.position - surfaceScanData.enterPoint).sqrMagnitude > lightComponent.range * lightComponent.range)
                        return(SurfaceScanData.toBeIgnored);
                    else
                    {
                        surfaceScanData.surfacesToScan = 3;
                        surfaceScanData.startVertex = new((column + 1) * Map.edgeLengthX_block, -Map.halfEdgeLengthY_block, row * Map.edgeLengthZ_block);
                        surfaceScanData.directionA = Vector3.up;
                        surfaceScanData.spacingA = Map.scanSpacingY_sphere_block;
                        surfaceScanData.stepsA = Map.scanStepsY_sphere_block;
                        surfaceScanData.directionB = Vector3.left;
                        surfaceScanData.spacingB = Map.scanSpacingX_sphere_block;
                        surfaceScanData.stepsB = Map.scanStepsX_sphere_block;
                        surfaceScanData.directionC = Vector3.forward;
                        surfaceScanData.spacingC = Map.scanSpacingZ_sphere_block;
                        surfaceScanData.stepsC = Map.scanStepsZ_sphere_block;
                    }
                }
                else//X+Y-Z=
                {
                    surfaceScanData.enterPoint = new((column + 1) * Map.edgeLengthX_block, -Map.halfEdgeLengthY_block, lightTrans.position.z);
                    if ((lightTrans.position - surfaceScanData.enterPoint).sqrMagnitude > lightComponent.range * lightComponent.range)
                        return(SurfaceScanData.toBeIgnored);
                    else
                    {
                        surfaceScanData.surfacesToScan = 2;
                        surfaceScanData.startVertex = new((column + 1) * Map.edgeLengthX_block, -Map.halfEdgeLengthY_block, row * Map.edgeLengthZ_block);
                        surfaceScanData.directionA = Vector3.forward;
                        surfaceScanData.spacingA = Map.scanSpacingZ_sphere_block;
                        surfaceScanData.stepsA = Map.scanStepsZ_sphere_block;
                        surfaceScanData.directionB = Vector3.left;
                        surfaceScanData.spacingB = Map.scanSpacingX_sphere_block;
                        surfaceScanData.stepsB = Map.scanStepsX_sphere_block;
                        surfaceScanData.directionC = Vector3.up;
                        surfaceScanData.spacingC = Map.scanSpacingY_sphere_block;
                        surfaceScanData.stepsC = Map.scanStepsY_sphere_block;
                    }
                }
            }
            else//X+Y=
            {
                if (lightTrans.position.z > (row + 1) * Map.edgeLengthZ_block)//X+Y=Z+
                {
                    surfaceScanData.enterPoint = new((column + 1) * Map.edgeLengthX_block, lightTrans.position.y, (row + 1) * Map.edgeLengthZ_block);
                    if ((lightTrans.position - surfaceScanData.enterPoint).sqrMagnitude > lightComponent.range * lightComponent.range)
                        return(SurfaceScanData.toBeIgnored);
                    else
                    {
                        surfaceScanData.surfacesToScan = 2;
                        surfaceScanData.startVertex = new((column + 1) * Map.edgeLengthX_block, Map.halfEdgeLengthY_block, row * Map.edgeLengthZ_block);
                        surfaceScanData.directionA = Vector3.up;
                        surfaceScanData.spacingA = Map.scanSpacingY_sphere_block;
                        surfaceScanData.stepsA = Map.scanStepsY_sphere_block;
                        surfaceScanData.directionB = Vector3.left;
                        surfaceScanData.spacingB = Map.scanSpacingX_sphere_block;
                        surfaceScanData.stepsB = Map.scanStepsX_sphere_block;
                        surfaceScanData.directionC = Vector3.back;
                        surfaceScanData.spacingC = Map.scanSpacingZ_sphere_block;
                        surfaceScanData.stepsC = Map.scanStepsZ_sphere_block;
                    }
                }
                else if (lightTrans.position.z < row * Map.edgeLengthZ_block)//X+Y=Z-
                {
                    surfaceScanData.enterPoint = new((column + 1) * Map.edgeLengthX_block, lightTrans.position.y, row * Map.edgeLengthZ_block);
                    if ((lightTrans.position - surfaceScanData.enterPoint).sqrMagnitude > lightComponent.range * lightComponent.range)
                        return(SurfaceScanData.toBeIgnored);
                    else
                    {
                        surfaceScanData.surfacesToScan = 2;
                        surfaceScanData.startVertex = new((column + 1) * Map.edgeLengthX_block, -Map.halfEdgeLengthY_block, row * Map.edgeLengthZ_block);
                        surfaceScanData.directionA = Vector3.up;
                        surfaceScanData.spacingA = Map.scanSpacingY_sphere_block;
                        surfaceScanData.stepsA = Map.scanStepsY_sphere_block;
                        surfaceScanData.directionB = Vector3.left;
                        surfaceScanData.spacingB = Map.scanSpacingX_sphere_block;
                        surfaceScanData.stepsB = Map.scanStepsX_sphere_block;
                        surfaceScanData.directionC = Vector3.forward;
                        surfaceScanData.spacingC = Map.scanSpacingZ_sphere_block;
                        surfaceScanData.stepsC = Map.scanStepsZ_sphere_block;
                    }
                }
                else//X+Y=Z=
                {
                    surfaceScanData.enterPoint = new((column + 1) * Map.edgeLengthX_block, lightTrans.position.y, lightTrans.position.z);
                    if ((lightTrans.position - surfaceScanData.enterPoint).sqrMagnitude > lightComponent.range * lightComponent.range)
                        return(SurfaceScanData.toBeIgnored);
                    else
                    {
                        surfaceScanData.surfacesToScan = 1;
                        surfaceScanData.startVertex = new((column + 1) * Map.edgeLengthX_block, Map.halfEdgeLengthY_block, row * Map.edgeLengthZ_block);
                        surfaceScanData.directionA = Vector3.down;
                        surfaceScanData.spacingA = Map.scanSpacingY_sphere_block;
                        surfaceScanData.stepsA = Map.scanStepsY_sphere_block;
                        surfaceScanData.directionB = Vector3.forward;
                        surfaceScanData.spacingB = Map.scanSpacingZ_sphere_block;
                        surfaceScanData.stepsB = Map.scanStepsZ_sphere_block;
                        surfaceScanData.directionC = Vector3.left;
                        surfaceScanData.spacingC = Map.scanSpacingX_sphere_block;
                        surfaceScanData.stepsC = Map.scanStepsX_sphere_block;
                    }
                }
            }
        }
        else if (lightTrans.position.x < column * Map.edgeLengthX_block)//X-
        {
            if (lightTrans.position.y > Map.halfEdgeLengthY_block)//X-Y+
            {
                if (lightTrans.position.z > (row + 1) * Map.edgeLengthZ_block)//X-Y+Z+
                {
                    surfaceScanData.enterPoint = new(column * Map.edgeLengthX_block, Map.halfEdgeLengthY_block, (row + 1) * Map.edgeLengthZ_block);
                    if ((lightTrans.position - surfaceScanData.enterPoint).sqrMagnitude > lightComponent.range * lightComponent.range)
                        return(SurfaceScanData.toBeIgnored);
                    else
                    {
                        surfaceScanData.surfacesToScan = 3;
                        surfaceScanData.startVertex = new(column * Map.edgeLengthX_block, Map.halfEdgeLengthY_block, (row + 1) * Map.edgeLengthZ_block);
                        surfaceScanData.directionA = Vector3.down;
                        surfaceScanData.spacingA = Map.scanSpacingY_sphere_block;
                        surfaceScanData.stepsA = Map.scanStepsY_sphere_block;
                        surfaceScanData.directionB = Vector3.right;
                        surfaceScanData.spacingB = Map.scanSpacingX_sphere_block;
                        surfaceScanData.stepsB = Map.scanStepsX_sphere_block;
                        surfaceScanData.directionC = Vector3.back;
                        surfaceScanData.spacingC = Map.scanSpacingZ_sphere_block;
                        surfaceScanData.stepsC = Map.scanStepsZ_sphere_block;
                    }
                }
                else if (lightTrans.position.z < row * Map.edgeLengthZ_block)//X-Y+Z-
                {
                    surfaceScanData.enterPoint = new(column * Map.edgeLengthX_block, Map.halfEdgeLengthY_block, row * Map.edgeLengthZ_block);
                    if ((lightTrans.position - surfaceScanData.enterPoint).sqrMagnitude > lightComponent.range * lightComponent.range)
                        return(SurfaceScanData.toBeIgnored);
                    else
                    {
                        surfaceScanData.surfacesToScan = 3;
                        surfaceScanData.startVertex = new(column * Map.edgeLengthX_block, Map.halfEdgeLengthY_block, row * Map.edgeLengthZ_block);
                        surfaceScanData.directionA = Vector3.down;
                        surfaceScanData.spacingA = Map.scanSpacingY_sphere_block;
                        surfaceScanData.stepsA = Map.scanStepsY_sphere_block;
                        surfaceScanData.directionB = Vector3.right;
                        surfaceScanData.spacingB = Map.scanSpacingX_sphere_block;
                        surfaceScanData.stepsB = Map.scanStepsX_sphere_block;
                        surfaceScanData.directionC = Vector3.forward;
                        surfaceScanData.spacingC = Map.scanSpacingZ_sphere_block;
                        surfaceScanData.stepsC = Map.scanStepsZ_sphere_block;
                    }
                }
                else//X-Y+Z=
                {
                    surfaceScanData.enterPoint = new(column * Map.edgeLengthX_block, Map.halfEdgeLengthY_block, lightTrans.position.z);
                    if ((lightTrans.position - surfaceScanData.enterPoint).sqrMagnitude > lightComponent.range * lightComponent.range)
                        return(SurfaceScanData.toBeIgnored);
                    else
                    {
                        surfaceScanData.surfacesToScan = 2;
                        surfaceScanData.startVertex = new(column * Map.edgeLengthX_block, Map.halfEdgeLengthY_block, row * Map.edgeLengthZ_block);
                        surfaceScanData.directionA = Vector3.forward;
                        surfaceScanData.spacingA = Map.scanSpacingZ_sphere_block;
                        surfaceScanData.stepsA = Map.scanStepsZ_sphere_block;
                        surfaceScanData.directionB = Vector3.right;
                        surfaceScanData.spacingB = Map.scanSpacingX_sphere_block;
                        surfaceScanData.stepsB = Map.scanStepsX_sphere_block;
                        surfaceScanData.directionC = Vector3.down;
                        surfaceScanData.spacingC = Map.scanSpacingY_sphere_block;
                        surfaceScanData.stepsC = Map.scanStepsY_sphere_block;
                    }
                }
            }
            else if (lightTrans.position.y < -Map.halfEdgeLengthY_block)//X-Y-
            {
                if (lightTrans.position.z > (row + 1) * Map.edgeLengthZ_block)//X-Y-Z+
                {
                    surfaceScanData.enterPoint = new(column * Map.edgeLengthX_block, -Map.halfEdgeLengthY_block, (row + 1) * Map.edgeLengthZ_block);
                    if ((lightTrans.position - surfaceScanData.enterPoint).sqrMagnitude > lightComponent.range * lightComponent.range)
                        return(SurfaceScanData.toBeIgnored);
                    else
                    {
                        surfaceScanData.surfacesToScan = 3;
                        surfaceScanData.startVertex = new(column * Map.edgeLengthX_block, -Map.halfEdgeLengthY_block, (row + 1) * Map.edgeLengthZ_block);
                        surfaceScanData.directionA = Vector3.up;
                        surfaceScanData.spacingA = Map.scanSpacingY_sphere_block;
                        surfaceScanData.stepsA = Map.scanStepsY_sphere_block;
                        surfaceScanData.directionB = Vector3.right;
                        surfaceScanData.spacingB = Map.scanSpacingX_sphere_block;
                        surfaceScanData.stepsB = Map.scanStepsX_sphere_block;
                        surfaceScanData.directionC = Vector3.back;
                        surfaceScanData.spacingC = Map.scanSpacingZ_sphere_block;
                        surfaceScanData.stepsC = Map.scanStepsZ_sphere_block;
                    }
                }
                else if (lightTrans.position.z < row * Map.edgeLengthZ_block)//X-Y-Z-
                {
                    surfaceScanData.enterPoint = new(column * Map.edgeLengthX_block, -Map.halfEdgeLengthY_block, row * Map.edgeLengthZ_block);
                    if ((lightTrans.position - surfaceScanData.enterPoint).sqrMagnitude > lightComponent.range * lightComponent.range)
                        return(SurfaceScanData.toBeIgnored);
                    else
                    {
                        surfaceScanData.surfacesToScan = 3;
                        surfaceScanData.startVertex = new(column * Map.edgeLengthX_block, -Map.halfEdgeLengthY_block, row * Map.edgeLengthZ_block);
                        surfaceScanData.directionA = Vector3.up;
                        surfaceScanData.spacingA = Map.scanSpacingY_sphere_block;
                        surfaceScanData.stepsA = Map.scanStepsY_sphere_block;
                        surfaceScanData.directionB = Vector3.right;
                        surfaceScanData.spacingB = Map.scanSpacingX_sphere_block;
                        surfaceScanData.stepsB = Map.scanStepsX_sphere_block;
                        surfaceScanData.directionC = Vector3.forward;
                        surfaceScanData.spacingC = Map.scanSpacingZ_sphere_block;
                        surfaceScanData.stepsC = Map.scanStepsZ_sphere_block;
                    }
                }
                else//X-Y-Z=
                {
                    surfaceScanData.enterPoint = new(column * Map.edgeLengthX_block, -Map.halfEdgeLengthY_block, lightTrans.position.z);
                    if ((lightTrans.position - surfaceScanData.enterPoint).sqrMagnitude > lightComponent.range * lightComponent.range)
                        return(SurfaceScanData.toBeIgnored);
                    else
                    {
                        surfaceScanData.surfacesToScan = 2;
                        surfaceScanData.startVertex = new(column * Map.edgeLengthX_block, -Map.halfEdgeLengthY_block, row * Map.edgeLengthZ_block);
                        surfaceScanData.directionA = Vector3.forward;
                        surfaceScanData.spacingA = Map.scanSpacingZ_sphere_block;
                        surfaceScanData.stepsA = Map.scanStepsZ_sphere_block;
                        surfaceScanData.directionB = Vector3.up;
                        surfaceScanData.spacingB = Map.scanSpacingY_sphere_block;
                        surfaceScanData.stepsB = Map.scanStepsY_sphere_block;
                        surfaceScanData.directionC = Vector3.right;
                        surfaceScanData.spacingC = Map.scanSpacingX_sphere_block;
                        surfaceScanData.stepsC = Map.scanStepsX_sphere_block;
                    }
                }
            }
            else//X-Y=
            {
                if (lightTrans.position.z > (row + 1) * Map.edgeLengthZ_block)//X-Y=Z+
                {
                    surfaceScanData.enterPoint = new(column * Map.edgeLengthX_block, lightTrans.position.y, (row + 1) * Map.edgeLengthZ_block);
                    if ((lightTrans.position - surfaceScanData.enterPoint).sqrMagnitude > lightComponent.range * lightComponent.range)
                        return(SurfaceScanData.toBeIgnored);
                    else
                    {
                        surfaceScanData.surfacesToScan = 2;
                        surfaceScanData.startVertex = new(column * Map.edgeLengthX_block, -Map.halfEdgeLengthY_block, (row + 1) * Map.edgeLengthZ_block);
                        surfaceScanData.directionA = Vector3.up;
                        surfaceScanData.spacingA = Map.scanSpacingY_sphere_block;
                        surfaceScanData.stepsA = Map.scanStepsY_sphere_block;
                        surfaceScanData.directionB = Vector3.right;
                        surfaceScanData.spacingB = Map.scanSpacingX_sphere_block;
                        surfaceScanData.stepsB = Map.scanStepsX_sphere_block;
                        surfaceScanData.directionC = Vector3.back;
                        surfaceScanData.spacingC = Map.scanSpacingZ_sphere_block;
                        surfaceScanData.stepsC = Map.scanStepsZ_sphere_block;
                    }
                }
                else if (lightTrans.position.z < row * Map.edgeLengthZ_block)//X-Y=Z-
                {
                    surfaceScanData.enterPoint = new(column * Map.edgeLengthX_block, lightTrans.position.y, row * Map.edgeLengthZ_block);
                    if ((lightTrans.position - surfaceScanData.enterPoint).sqrMagnitude > lightComponent.range * lightComponent.range)
                        return(SurfaceScanData.toBeIgnored);
                    else
                    {
                        surfaceScanData.surfacesToScan = 2;
                        surfaceScanData.startVertex = new(column * Map.edgeLengthX_block, -Map.halfEdgeLengthY_block, row * Map.edgeLengthZ_block);
                        surfaceScanData.directionA = Vector3.up;
                        surfaceScanData.spacingA = Map.scanSpacingY_sphere_block;
                        surfaceScanData.stepsA = Map.scanStepsY_sphere_block;
                        surfaceScanData.directionB = Vector3.right;
                        surfaceScanData.spacingB = Map.scanSpacingX_sphere_block;
                        surfaceScanData.stepsB = Map.scanStepsX_sphere_block;
                        surfaceScanData.directionC = Vector3.forward;
                        surfaceScanData.spacingC = Map.scanSpacingZ_sphere_block;
                        surfaceScanData.stepsC = Map.scanStepsZ_sphere_block;
                    }
                }
                else//X-Y=Z=
                {
                    surfaceScanData.enterPoint = new(column * Map.edgeLengthX_block, lightTrans.position.y, lightTrans.position.z);
                    if ((lightTrans.position - surfaceScanData.enterPoint).sqrMagnitude > lightComponent.range * lightComponent.range)
                        return(SurfaceScanData.toBeIgnored);
                    else
                    {
                        surfaceScanData.surfacesToScan = 1;
                        surfaceScanData.startVertex = new(column * Map.edgeLengthX_block, -Map.halfEdgeLengthY_block, row * Map.edgeLengthZ_block);
                        surfaceScanData.directionA = Vector3.up;
                        surfaceScanData.spacingA = Map.scanSpacingY_sphere_block;
                        surfaceScanData.stepsA = Map.scanStepsY_sphere_block;
                        surfaceScanData.directionB = Vector3.forward;
                        surfaceScanData.spacingB = Map.scanSpacingZ_sphere_block;
                        surfaceScanData.stepsB = Map.scanStepsZ_sphere_block;
                        surfaceScanData.directionC = Vector3.right;
                        surfaceScanData.spacingC = Map.scanSpacingX_sphere_block;
                        surfaceScanData.stepsC = Map.scanStepsX_sphere_block;
                    }
                }
            }
        }
        else//X=
        {
            if (lightTrans.position.y > Map.halfEdgeLengthY_block)//X=Y+
            {
                if (lightTrans.position.z > (row + 1) * Map.edgeLengthZ_block)//X=Y+Z+
                {
                    surfaceScanData.enterPoint = new(lightTrans.position.x, Map.halfEdgeLengthY_block, (row + 1) * Map.edgeLengthZ_block);
                    if ((lightTrans.position - surfaceScanData.enterPoint).sqrMagnitude > lightComponent.range * lightComponent.range)
                        return(SurfaceScanData.toBeIgnored);
                    else
                    {
                        surfaceScanData.surfacesToScan = 2;
                        surfaceScanData.startVertex = new(column * Map.edgeLengthX_block, Map.halfEdgeLengthY_block, (row + 1) * Map.edgeLengthZ_block);
                        surfaceScanData.directionA = Vector3.right;
                        surfaceScanData.spacingA = Map.scanSpacingX_sphere_block;
                        surfaceScanData.stepsA = Map.scanStepsX_sphere_block;
                        surfaceScanData.directionB = Vector3.down;
                        surfaceScanData.spacingB = Map.scanSpacingY_sphere_block;
                        surfaceScanData.stepsB = Map.scanStepsY_sphere_block;
                        surfaceScanData.directionC = Vector3.back;
                        surfaceScanData.spacingC = Map.scanSpacingZ_sphere_block;
                        surfaceScanData.stepsC = Map.scanStepsZ_sphere_block;
                    }
                }
                else if (lightTrans.position.z < row * Map.edgeLengthZ_block)//X=Y+Z-
                {
                    surfaceScanData.enterPoint = new(lightTrans.position.x, Map.halfEdgeLengthY_block, row * Map.edgeLengthZ_block);
                    if ((lightTrans.position - surfaceScanData.enterPoint).sqrMagnitude > lightComponent.range * lightComponent.range)
                        return(SurfaceScanData.toBeIgnored);
                    else
                    {
                        surfaceScanData.surfacesToScan = 2;
                        surfaceScanData.startVertex = new(column * Map.edgeLengthX_block, Map.halfEdgeLengthY_block, row * Map.edgeLengthZ_block);
                        surfaceScanData.directionA = Vector3.right;
                        surfaceScanData.spacingA = Map.scanSpacingX_sphere_block;
                        surfaceScanData.stepsA = Map.scanStepsX_sphere_block;
                        surfaceScanData.directionB = Vector3.down;
                        surfaceScanData.spacingB = Map.scanSpacingY_sphere_block;
                        surfaceScanData.stepsB = Map.scanStepsY_sphere_block;
                        surfaceScanData.directionC = Vector3.forward;
                        surfaceScanData.spacingC = Map.scanSpacingZ_sphere_block;
                        surfaceScanData.stepsC = Map.scanStepsZ_sphere_block;
                    }
                }
                else//X=Y+Z=
                {
                    surfaceScanData.enterPoint = new(lightTrans.position.x, Map.halfEdgeLengthY_block, lightTrans.position.z);
                    if ((lightTrans.position - surfaceScanData.enterPoint).sqrMagnitude > lightComponent.range * lightComponent.range)
                        return(SurfaceScanData.toBeIgnored);
                    else
                    {
                        surfaceScanData.surfacesToScan = 1;
                        surfaceScanData.startVertex = new(column * Map.edgeLengthX_block, Map.halfEdgeLengthY_block, row * Map.edgeLengthZ_block);
                        surfaceScanData.directionA = Vector3.right;
                        surfaceScanData.spacingA = Map.scanSpacingX_sphere_block;
                        surfaceScanData.stepsA = Map.scanStepsX_sphere_block;
                        surfaceScanData.directionB = Vector3.up;
                        surfaceScanData.spacingB = Map.scanSpacingY_sphere_block;
                        surfaceScanData.stepsB = Map.scanStepsY_sphere_block;
                        surfaceScanData.directionC = Vector3.down;
                        surfaceScanData.spacingC = Map.scanSpacingZ_sphere_block;
                        surfaceScanData.stepsC = Map.scanStepsZ_sphere_block;
                    }
                }
            }
            else if (lightTrans.position.y < -Map.halfEdgeLengthY_block)//X=Y-
            {
                if (lightTrans.position.z > (row + 1) * Map.edgeLengthZ_block)//X=Y-Z+
                {
                    surfaceScanData.enterPoint = new(lightTrans.position.x, -Map.halfEdgeLengthY_block, (row + 1) * Map.edgeLengthZ_block);
                    if ((lightTrans.position - surfaceScanData.enterPoint).sqrMagnitude > lightComponent.range * lightComponent.range)
                        return(SurfaceScanData.toBeIgnored);
                    else
                    {
                        surfaceScanData.surfacesToScan = 2;
                        surfaceScanData.startVertex = new(column * Map.edgeLengthX_block, -Map.halfEdgeLengthY_block, (row + 1) * Map.edgeLengthZ_block);
                        surfaceScanData.directionA = Vector3.right;
                        surfaceScanData.spacingA = Map.scanSpacingX_sphere_block;
                        surfaceScanData.stepsA = Map.scanStepsX_sphere_block;
                        surfaceScanData.directionB = Vector3.up;
                        surfaceScanData.spacingB = Map.scanSpacingY_sphere_block;
                        surfaceScanData.stepsB = Map.scanStepsY_sphere_block;
                        surfaceScanData.directionC = Vector3.back;
                        surfaceScanData.spacingC = Map.scanSpacingZ_sphere_block;
                        surfaceScanData.stepsC = Map.scanStepsZ_sphere_block;
                    }
                }
                else if (lightTrans.position.z < row * Map.edgeLengthZ_block)//X=Y-Z-
                {
                    surfaceScanData.enterPoint = new(lightTrans.position.x, -Map.halfEdgeLengthY_block, row * Map.edgeLengthZ_block);
                    if ((lightTrans.position - surfaceScanData.enterPoint).sqrMagnitude > lightComponent.range * lightComponent.range)
                        return(SurfaceScanData.toBeIgnored);
                    else
                    {
                        surfaceScanData.surfacesToScan = 2;
                        surfaceScanData.startVertex = new(column * Map.edgeLengthX_block, -Map.halfEdgeLengthY_block, row * Map.edgeLengthZ_block);
                        surfaceScanData.directionA = Vector3.right;
                        surfaceScanData.spacingA = Map.scanSpacingX_sphere_block;
                        surfaceScanData.stepsA = Map.scanStepsX_sphere_block;
                        surfaceScanData.directionB = Vector3.up;
                        surfaceScanData.spacingB = Map.scanSpacingY_sphere_block;
                        surfaceScanData.stepsB = Map.scanStepsY_sphere_block;
                        surfaceScanData.directionC = Vector3.forward;
                        surfaceScanData.spacingC = Map.scanSpacingZ_sphere_block;
                        surfaceScanData.stepsC = Map.scanStepsZ_sphere_block;
                    }
                }
                else//X=Y-Z=
                {
                    surfaceScanData.enterPoint = new(lightTrans.position.x, -Map.halfEdgeLengthY_block, lightTrans.position.z);
                    if ((lightTrans.position - surfaceScanData.enterPoint).sqrMagnitude > lightComponent.range * lightComponent.range)
                        return(SurfaceScanData.toBeIgnored);
                    else
                    {
                        surfaceScanData.surfacesToScan = 1;
                        surfaceScanData.startVertex = new(column * Map.edgeLengthX_block, -Map.halfEdgeLengthY_block, row * Map.edgeLengthZ_block);
                        surfaceScanData.directionA = Vector3.right;
                        surfaceScanData.spacingA = Map.scanSpacingX_sphere_block;
                        surfaceScanData.stepsA = Map.scanStepsX_sphere_block;
                        surfaceScanData.directionB = Vector3.forward;
                        surfaceScanData.spacingB = Map.scanSpacingZ_sphere_block;
                        surfaceScanData.stepsB = Map.scanStepsZ_sphere_block;
                        surfaceScanData.directionC = Vector3.up;
                        surfaceScanData.spacingC = Map.scanSpacingY_sphere_block;
                        surfaceScanData.stepsC = Map.scanStepsY_sphere_block;
                    }
                }
            }
            else//X=Y=
            {
                if (lightTrans.position.z > (row + 1) * Map.edgeLengthZ_block)//X=Y=Z+
                {
                    surfaceScanData.enterPoint = new(lightTrans.position.x, lightTrans.position.y, (row + 1) * Map.edgeLengthZ_block);
                    if ((lightTrans.position - surfaceScanData.enterPoint).sqrMagnitude > lightComponent.range * lightComponent.range)
                        return(SurfaceScanData.toBeIgnored);
                    else
                    {
                        surfaceScanData.surfacesToScan = 1;
                        surfaceScanData.startVertex = new(column * Map.edgeLengthX_block, -Map.halfEdgeLengthY_block, (row + 1) * Map.edgeLengthZ_block);
                        surfaceScanData.directionA = Vector3.right;
                        surfaceScanData.spacingA = Map.scanSpacingX_sphere_block;
                        surfaceScanData.stepsA = Map.scanStepsX_sphere_block;
                        surfaceScanData.directionB = Vector3.up;
                        surfaceScanData.spacingB = Map.scanSpacingY_sphere_block;
                        surfaceScanData.stepsB = Map.scanStepsY_sphere_block;
                        surfaceScanData.directionC = Vector3.forward;
                        surfaceScanData.spacingC = Map.scanSpacingZ_sphere_block;
                        surfaceScanData.stepsC = Map.scanStepsZ_sphere_block;
                    }
                }
                else if (lightTrans.position.z < row * Map.edgeLengthZ_block)//X=Y=Z-
                {
                    surfaceScanData.enterPoint = new(lightTrans.position.x, lightTrans.position.y, row * Map.edgeLengthZ_block);
                    if ((lightTrans.position - surfaceScanData.enterPoint).sqrMagnitude > lightComponent.range * lightComponent.range)
                        return(SurfaceScanData.toBeIgnored);
                    else
                    {
                        surfaceScanData.surfacesToScan = 1;
                        surfaceScanData.startVertex = new(column * Map.edgeLengthX_block, -Map.halfEdgeLengthY_block, row * Map.edgeLengthZ_block);
                        surfaceScanData.directionA = Vector3.right;
                        surfaceScanData.spacingA = Map.scanSpacingX_sphere_block;
                        surfaceScanData.stepsA = Map.scanStepsX_sphere_block;
                        surfaceScanData.directionB = Vector3.up;
                        surfaceScanData.spacingB = Map.scanSpacingY_sphere_block;
                        surfaceScanData.stepsB = Map.scanStepsY_sphere_block;
                        surfaceScanData.directionC = Vector3.forward;
                        surfaceScanData.spacingC = Map.scanSpacingZ_sphere_block;
                        surfaceScanData.stepsC = Map.scanStepsZ_sphere_block;
                    }
                }
                else//X=Y=Z=
                {
                    surfaceScanData.surfacesToScan = 0;
                    surfaceScanData.enterPoint = lightTrans.position;//LineCast起点和终点相同

                    surfaceScanData.startVertex = Vector3.zero;
                    surfaceScanData.directionA = Vector3.zero;
                    surfaceScanData.spacingA = 0f;
                    surfaceScanData.stepsA = 0;
                    surfaceScanData.directionB = Vector3.zero;
                    surfaceScanData.spacingB = 0f;
                    surfaceScanData.stepsB = 0;
                    surfaceScanData.directionC = Vector3.zero;
                    surfaceScanData.spacingC = 0f;
                    surfaceScanData.stepsC = 0;

                }
            }
        }

        return surfaceScanData;
    }

    public void DeployBlock(Block thisBlock)
    {
        blocksCoveredByThis_now.Add(thisBlock);

        //判断是否在旧覆盖范围内
        int indexInPreviousCoverage = blocksCoveredByThis_previous.IndexOf(thisBlock);
        if (indexInPreviousCoverage != -1)//在旧覆盖范围内，从blocksCoveredByThis_previous中抽离（此列表最终留下的block就是失去照射、需要清理的block）
        {
            blocksCoveredByThis_previous[indexInPreviousCoverage] = blocksCoveredByThis_previous[^1];
            blocksCoveredByThis_previous.RemoveAt(blocksCoveredByThis_previous.Count - 1);
        }
        else//不在旧覆盖范围内
        {
            thisBlock.num_lampsCoveringThis += 1;
            if (thisBlock.num_lampsCoveringThis == 1 && solidProbability != 0f && thisBlock.brick == null)//可能最后一个判断项性能消耗高？
            {
                if (solidProbability == 1f || UnityEngine.Random.Range(0f, 1f) <= solidProbability)
                {
                    if (brickSource == -1)
                    {
                        thisBlock.brick = Pool.exnBricks[^1];//由缓存池确保引用不为null
                        Pool.exnBricks.RemoveAt(Pool.exnBricks.Count - 1);
                        thisBlock.brick.transform.position = thisBlock.CenterPos();
                    }
                    else
                        thisBlock.brick = GameObject.Instantiate(Prefabs.relicBricks[brickSource], thisBlock.CenterPos(), Quaternion.identity);
                }
            }
        }
    }

    public void UpdateUncoveredBlocks()
    {
        foreach (Block thisBlock in blocksCoveredByThis_previous)
        {
            thisBlock.num_lampsCoveringThis -= 1;

            if (thisBlock.num_lampsCoveringThis == 0 && thisBlock.brick != null)//若block未被任何光源照射且不为虚空，回收其brick
            {
                if (thisBlock.brick.CompareTag("RelicBlockObject") == true)
                {
                    thisBlock.brick.GetComponent<MeshRenderer>().material = Prefabs.material_exnBrick;
                    thisBlock.brick.tag = "ExNBrick";
                    thisBlock.brick.transform.SetParent(Pool.parent_exnBricks.transform);
                }
                thisBlock.brick.transform.position = Pool.FAR_AWAY;
                Pool.exnBricks.Add(thisBlock.brick);
                thisBlock.brick = null;
            }
        }
    }

    public void UpdateGroundBlockCoverage_Sphere()
    {
        /*排除光源过高或过低以至于不可能照射地面的情况，粗略计算覆盖范围*/
        float radiusOnGround;
        if (lightTrans.position.y > Map.halfEdgeLengthY_block)
        {
            if (lightTrans.position.y > Map.halfEdgeLengthY_block + lightComponent.range)
                return;
            radiusOnGround = Mathf.Sqrt(lightComponent.range * lightComponent.range - (lightTrans.position.y - Map.halfEdgeLengthY_block) * (lightTrans.position.y - Map.halfEdgeLengthY_block));
        }
        else if (lightTrans.position.y < -Map.halfEdgeLengthY_block)
        {
            if (lightTrans.position.y < -Map.halfEdgeLengthY_block - lightComponent.range)
                return;
            radiusOnGround = Mathf.Sqrt(lightComponent.range * lightComponent.range - (lightTrans.position.y + Map.halfEdgeLengthY_block) * (lightTrans.position.y + Map.halfEdgeLengthY_block));
        }
        else
            radiusOnGround = lightComponent.range;

        int leftLimit = Mathf.FloorToInt((lightTrans.position.x - radiusOnGround) * Map.reciprocalEdgeLengthX_block);
        int rightLimit = Mathf.FloorToInt((lightTrans.position.x + radiusOnGround) * Map.reciprocalEdgeLengthX_block);
        int downLimit = Mathf.FloorToInt((lightTrans.position.z - radiusOnGround) * Map.reciprocalEdgeLengthZ_block);
        int upLimit = Mathf.FloorToInt((lightTrans.position.z + radiusOnGround) * Map.reciprocalEdgeLengthZ_block);

        /*精确计算覆盖范围，为LineCast设定参数，检测遮蔽，部署Block*/
        for (int column = leftLimit; column <= rightLimit; column += 1)
        {
            for (int row = downLimit; row <= upLimit; row += 1)
            {
                //判断光源相对于block的位置，判断是否在无遮挡条件下被光源照射，若是，设定LineCast检测参数
                SurfaceScanData surfaceScanData = SurfaceScanData_AsSphereLamp(column, row);
                if (surfaceScanData.surfacesToScan == -1)
                    continue;

                //对无遮挡条件下被光源照射的block做障碍物检测
                if (Physics.Linecast(lightTrans.position, surfaceScanData.enterPoint, 7) == false)
                    goto DeployBlock;

                if (surfaceScanData.surfacesToScan == 3)
                {
                    //两个辅轴从stepsB==1,setpsC==1开始交错扫描
                    for (int stepsB = 1; stepsB <= surfaceScanData.stepsB; stepsB += 1)
                    {
                        for (int stepsC = 1; stepsC <= surfaceScanData.stepsC; stepsC += 1)
                        {
                            Vector3 pos = surfaceScanData.startVertex + stepsB * surfaceScanData.spacingB * surfaceScanData.directionB + stepsC * surfaceScanData.spacingC * surfaceScanData.directionC;
                            if ((lightTrans.position - pos).sqrMagnitude <= lightComponent.range * lightComponent.range && Physics.Linecast(lightTrans.position, pos, 7) == false)
                                goto DeployBlock;
                        }
                    }
                    //主轴与第一辅助轴从stepsA==0,stepsB==0开始交错扫描，主轴与第二辅助轴从stepsA==0,stepsB==1开始交错扫描
                    for (int stepsA = 0; stepsA <= surfaceScanData.stepsA; stepsA += 1)
                    {
                        for (int stepsB = 0; stepsB <= surfaceScanData.stepsB; stepsB += 1)
                        {
                            Vector3 pos = surfaceScanData.startVertex + stepsA * surfaceScanData.spacingA * surfaceScanData.directionA + stepsB * surfaceScanData.spacingB * surfaceScanData.directionB;
                            if ((lightTrans.position - pos).sqrMagnitude <= lightComponent.range * lightComponent.range && Physics.Linecast(lightTrans.position, pos, 7) == false)
                                goto DeployBlock;
                        }
                        for (int stepsC = 1; stepsC <= surfaceScanData.stepsC; stepsC += 1)
                        {
                            Vector3 pos = surfaceScanData.startVertex + stepsA * surfaceScanData.spacingA * surfaceScanData.directionA + stepsC * surfaceScanData.spacingC * surfaceScanData.directionC;
                            if ((lightTrans.position - pos).sqrMagnitude <= lightComponent.range * lightComponent.range && Physics.Linecast(lightTrans.position, pos, 7) == false)
                                goto DeployBlock;
                        }
                    }
                }
                else if (surfaceScanData.surfacesToScan == 2)
                {
                    //主轴与第一辅助轴从stepsA == 0,stepsB == 0开始交错扫描，主轴与第二辅助轴从stepsA == 0,stepsB == 1开始交错扫描
                    for (int stepsA = 0; stepsA <= surfaceScanData.stepsA; stepsA += 1)
                    {
                        for (int stepsB = 0; stepsB <= surfaceScanData.stepsB; stepsB += 1)
                        {
                            Vector3 pos = surfaceScanData.startVertex + stepsA * surfaceScanData.spacingA * surfaceScanData.directionA + stepsB * surfaceScanData.spacingB * surfaceScanData.directionB;
                            if ((lightTrans.position - pos).sqrMagnitude <= lightComponent.range * lightComponent.range && Physics.Linecast(lightTrans.position, pos, 7) == false)
                                goto DeployBlock;
                        }
                        for (int stepsC = 1; stepsC <= surfaceScanData.stepsC; stepsC += 1)
                        {
                            Vector3 pos = surfaceScanData.startVertex + stepsA * surfaceScanData.spacingA * surfaceScanData.directionA + stepsC * surfaceScanData.spacingC * surfaceScanData.directionC;
                            if ((lightTrans.position - pos).sqrMagnitude <= lightComponent.range * lightComponent.range && Physics.Linecast(lightTrans.position, pos, 7) == false)
                                goto DeployBlock;
                        }
                    }
                }
                else if (surfaceScanData.surfacesToScan == 1)
                {
                    //主轴第一辅助轴从stepsA == 0,stepsB == 0开始交错扫描
                    for (int stepsA = 0; stepsA <= surfaceScanData.stepsA; stepsA += 1)
                    {
                        for (int stepsB = 0; stepsB <= surfaceScanData.stepsB; stepsB += 1)
                        {
                            Vector3 pos = surfaceScanData.startVertex + stepsA * surfaceScanData.spacingA * surfaceScanData.directionA + stepsB * surfaceScanData.spacingB * surfaceScanData.directionB;
                            if ((lightTrans.position - pos).sqrMagnitude <= lightComponent.range * lightComponent.range && Physics.Linecast(lightTrans.position, pos, 7) == false)
                                goto DeployBlock;
                        }
                    }
                }
                else//surfaceScanData.surfacesToScan == 0
                    goto DeployBlock;

                //若未跳转，即未被照射，进入下一个block迭代
                continue;

            DeployBlock:

                /*部署block*/
                DeployBlock(Map.blocks[column][row]);
            }
        }

        /*清理失去照射的block*/
        UpdateUncoveredBlocks();

        /*用新集合取代旧集合*/
        blocksCoveredByThis_previous.Clear();
        (blocksCoveredByThis_now, blocksCoveredByThis_previous) = (blocksCoveredByThis_previous, blocksCoveredByThis_now);
    }
    public void UpdateGroundBlockCoverage_Spot()
    {
        /*
         * 粗略计算覆盖范围
         */

        //待计算数值
        int leftLimit, rightLimit, downLimit, upLimit;

        //数值缓存
        Vector3 forwardDirection = lightTrans.forward;

        //back,front,right,left都根据forwardDirection_parallelToGround定义
        Vector3 forwardDirection_parallelToGround = new Vector3(forwardDirection.x, 0f, forwardDirection.z).normalized;

        if (forwardDirection_parallelToGround == Vector3.zero)//垂直地面照射，简化为圆形
        {
            float radiusOnGround;

            if (lightTrans.position.y > Map.halfEdgeLengthY_block)
            {
                if (forwardDirection.y > 0f)
                    return;

                float edgeRayExtention_vertical = lightComponent.range * Mathf.Cos(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad);

                if (lightTrans.position.y - edgeRayExtention_vertical < -Map.halfEdgeLengthY_block)
                    radiusOnGround = (lightTrans.position.y + Map.halfEdgeLengthY_block) * Mathf.Tan(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad);
                else if (lightTrans.position.y - edgeRayExtention_vertical <= Map.halfEdgeLengthY_block)
                    radiusOnGround = lightComponent.range * Mathf.Sin(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad);
                else
                    radiusOnGround = Mathf.Sqrt(lightComponent.range * lightComponent.range - (lightTrans.position.y - Map.halfEdgeLengthY_block) * (lightTrans.position.y - Map.halfEdgeLengthY_block));
            }
            else if (lightTrans.position.y < -Map.halfEdgeLengthY_block)
            {
                if (forwardDirection.y < 0f)
                    return;

                float edgeRayExtention_vertical = lightComponent.range * Mathf.Cos(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad);

                if (lightTrans.position.y + edgeRayExtention_vertical > Map.halfEdgeLengthY_block)
                    radiusOnGround = (Map.halfEdgeLengthY_block - lightTrans.position.y) * Mathf.Tan(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad);
                else if (lightTrans.position.y + edgeRayExtention_vertical >= -Map.halfEdgeLengthY_block)
                    radiusOnGround = lightComponent.range * Mathf.Sin(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad);
                else
                    radiusOnGround = Mathf.Sqrt(lightComponent.range * lightComponent.range - (-Map.halfEdgeLengthY_block - lightTrans.position.y) * (-Map.halfEdgeLengthY_block - lightTrans.position.y));
            }
            else
            {
                float edgeRayExtention_vertical = lightComponent.range * Mathf.Cos(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad);

                if (forwardDirection.y < 0f)
                {
                    if (lightTrans.position.y - edgeRayExtention_vertical < -Map.halfEdgeLengthY_block)
                        radiusOnGround = (lightTrans.position.y + Map.halfEdgeLengthY_block) * Mathf.Tan(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad);
                    else
                        radiusOnGround = lightComponent.range * Mathf.Sin(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad);
                }
                else
                {
                    if (lightTrans.position.y + edgeRayExtention_vertical > Map.halfEdgeLengthY_block)
                        radiusOnGround = (Map.halfEdgeLengthY_block - lightTrans.position.y) * Mathf.Tan(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad);
                    else
                        radiusOnGround = lightComponent.range * Mathf.Sin(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad);
                }
            }

            leftLimit = Mathf.FloorToInt((lightTrans.position.x - radiusOnGround) * Map.reciprocalEdgeLengthX_block);
            rightLimit = Mathf.FloorToInt((lightTrans.position.x + radiusOnGround) * Map.reciprocalEdgeLengthX_block);
            downLimit = Mathf.FloorToInt((lightTrans.position.z - radiusOnGround) * Map.reciprocalEdgeLengthZ_block);
            upLimit = Mathf.FloorToInt((lightTrans.position.z + radiusOnGround) * Map.reciprocalEdgeLengthZ_block);
        }
        else//非垂直照射地面
        {
            Vector3 cuttingEdgeInGround_front, cuttingEdgeInGround_back;//back与front不对称
            Vector3 cuttingEdgeInGround_left;//right与left对称，只需计算一个

            Vector3 rightDirection_parallelToGround = Vector3.Cross(Vector3.up, forwardDirection_parallelToGround);

            if (lightTrans.position.y > Map.halfEdgeLengthY_block)//光源在ground上方
            {
                Vector3 backRayDirection = Quaternion.AngleAxis(lightComponent.spotAngle * 0.5f, rightDirection_parallelToGround) * forwardDirection;
                Vector3 backRayEnd = lightTrans.position + backRayDirection * lightComponent.range;
                //计算cuttingEdgeInGround_back
                if (Vector3.Dot(backRayDirection, forwardDirection_parallelToGround) > 0f)//back在前
                {
                    if (backRayEnd.y <= Map.halfEdgeLengthY_block)
                        cuttingEdgeInGround_back = lightTrans.position + (lightTrans.position.y - Map.halfEdgeLengthY_block) / (lightTrans.position.y - backRayEnd.y) * lightComponent.range * backRayDirection;//浮点数精度隐患
                    else
                        return;
                }
                else//back在后或正下方
                {
                    if (backRayEnd.y < -Map.halfEdgeLengthY_block)
                        cuttingEdgeInGround_back = lightTrans.position + (lightTrans.position.y + Map.halfEdgeLengthY_block) / (lightTrans.position.y - backRayEnd.y) * lightComponent.range * backRayDirection;
                    else if (backRayEnd.y < Map.halfEdgeLengthY_block)
                        cuttingEdgeInGround_back = backRayEnd;
                    else
                        cuttingEdgeInGround_back = new Vector3(lightTrans.position.x, Map.halfEdgeLengthY_block, lightTrans.position.z) - Mathf.Sqrt(lightComponent.range * lightComponent.range - (lightTrans.position.y - Map.halfEdgeLengthY_block) * (lightTrans.position.y - Map.halfEdgeLengthY_block)) * forwardDirection_parallelToGround;
                }

                Vector3 frontRayDirection = Quaternion.AngleAxis(-lightComponent.spotAngle * 0.5f, rightDirection_parallelToGround) * forwardDirection;
                Vector3 frontRayEnd = lightTrans.position + frontRayDirection * lightComponent.range;
                //计算cuttingEdgeInGround_front
                if (frontRayEnd.y < -Map.halfEdgeLengthY_block)
                    cuttingEdgeInGround_front = lightTrans.position + (lightTrans.position.y + Map.halfEdgeLengthY_block) / (lightTrans.position.y - frontRayEnd.y) * lightComponent.range * frontRayDirection;
                else if (frontRayEnd.y < Map.halfEdgeLengthY_block)
                    cuttingEdgeInGround_front = frontRayEnd;
                else
                    cuttingEdgeInGround_front = new Vector3(lightTrans.position.x, Map.halfEdgeLengthY_block, lightTrans.position.z) + Mathf.Sqrt(lightComponent.range * lightComponent.range - (lightTrans.position.y - Map.halfEdgeLengthY_block) * (lightTrans.position.y - Map.halfEdgeLengthY_block)) * forwardDirection_parallelToGround;

                Vector3 leftRayDirection = Quaternion.AngleAxis(90f, forwardDirection) * backRayDirection;
                Vector3 leftRayEnd = lightTrans.position + leftRayDirection * lightComponent.range;
                //计算cuttingEdgeInGround_left
                if (leftRayEnd.y < -Map.halfEdgeLengthY_block)
                    cuttingEdgeInGround_left = lightTrans.position + (lightTrans.position.y + Map.halfEdgeLengthY_block) / (lightTrans.position.y - leftRayEnd.y) * lightComponent.range * leftRayDirection;
                else if (leftRayEnd.y < Map.halfEdgeLengthY_block)
                    cuttingEdgeInGround_left = leftRayEnd;
                else
                {
                    //中心光线通过光锥“平底”的点
                    Vector3 circleCenterPos = lightTrans.position + Mathf.Cos(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad) * lightComponent.range * forwardDirection;
                    //cuttingEdgeInGround_left与 circleCenterPos到leftRayEnd的连线 的垂线距离可知，与circleCenterPos的距离可知，求 其与circleCenterPos的连线 与 circleCenterPos与leftRayEnd的连线（平底圆形水平半径） 的夹角
                    float distancePerpendicular = (circleCenterPos.y - Map.halfEdgeLengthY_block) / Vector3.Cross(forwardDirection, rightDirection_parallelToGround).y;
                    float circleRadius = Mathf.Sin(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad) * lightComponent.range;

                    float divideValue = distancePerpendicular / circleRadius;
                    float angleRad;
                    if (divideValue > 1f)
                    {
                        Debug.Log("Error! divideValue > 1f! distancePerpendicular == " + distancePerpendicular + "; Vector3.Cross(forwardDirection, rightDirection_parallelToGround).y == " + Vector3.Cross(forwardDirection, rightDirection_parallelToGround).y);
                        angleRad = Mathf.PI * 0.5f;
                    }
                    else if (divideValue < -1f)
                    {
                        Debug.Log("Error! divideValue < -1f! distancePerpendicular == " + distancePerpendicular + "; Vector3.Cross(forwardDirection, rightDirection_parallelToGround).y == " + Vector3.Cross(forwardDirection, rightDirection_parallelToGround).y);
                        angleRad = Mathf.PI * 0.5f;
                    }
                    else
                        angleRad = Mathf.Asin(divideValue);

                    //将 由circleCenterPos指向leftRayEnd的向量 沿着forward轴顺时针旋转angle角度，获得cuttingEdgeInGround_left
                    cuttingEdgeInGround_left = circleCenterPos + Quaternion.AngleAxis(angleRad * Mathf.Rad2Deg, forwardDirection) * (leftRayEnd - circleCenterPos);
                }
            }
            else if (lightTrans.position.y < -Map.halfEdgeLengthY_block)//光源在ground下方
            {
                Vector3 backRayDirection = Quaternion.AngleAxis(-lightComponent.spotAngle * 0.5f, rightDirection_parallelToGround) * forwardDirection;
                Vector3 backRayEnd = lightTrans.position + backRayDirection * lightComponent.range;
                //计算cuttingEdgeInGround_back
                if (Vector3.Dot(backRayDirection, forwardDirection_parallelToGround) > 0f)//back在前
                {
                    if (backRayEnd.y >= -Map.halfEdgeLengthY_block)
                        cuttingEdgeInGround_back = lightTrans.position + (lightTrans.position.y + Map.halfEdgeLengthY_block) / (lightTrans.position.y - backRayEnd.y) * lightComponent.range * backRayDirection;//浮点数精度隐患
                    else
                        return;
                }
                else//back在后或正下方
                {
                    if (backRayEnd.y > Map.halfEdgeLengthY_block)
                        cuttingEdgeInGround_back = lightTrans.position + (lightTrans.position.y - Map.halfEdgeLengthY_block) / (lightTrans.position.y - backRayEnd.y) * lightComponent.range * backRayDirection;
                    else if (backRayEnd.y > -Map.halfEdgeLengthY_block)
                        cuttingEdgeInGround_back = backRayEnd;
                    else
                        cuttingEdgeInGround_back = new Vector3(lightTrans.position.x, -Map.halfEdgeLengthY_block, lightTrans.position.z) - Mathf.Sqrt(lightComponent.range * lightComponent.range - (lightTrans.position.y + Map.halfEdgeLengthY_block) * (lightTrans.position.y + Map.halfEdgeLengthY_block)) * forwardDirection_parallelToGround;
                }

                Vector3 frontRayDirection = Quaternion.AngleAxis(lightComponent.spotAngle * 0.5f, rightDirection_parallelToGround) * forwardDirection;
                Vector3 frontRayEnd = lightTrans.position + frontRayDirection * lightComponent.range;
                //计算cuttingEdgeInGround_front
                if (frontRayEnd.y > Map.halfEdgeLengthY_block)
                    cuttingEdgeInGround_front = lightTrans.position + (lightTrans.position.y - Map.halfEdgeLengthY_block) / (lightTrans.position.y - frontRayEnd.y) * lightComponent.range * frontRayDirection;
                else if (frontRayEnd.y > -Map.halfEdgeLengthY_block)
                    cuttingEdgeInGround_front = frontRayEnd;
                else
                    cuttingEdgeInGround_front = new Vector3(lightTrans.position.x, -Map.halfEdgeLengthY_block, lightTrans.position.z) + Mathf.Sqrt(lightComponent.range * lightComponent.range - (lightTrans.position.y + Map.halfEdgeLengthY_block) * (lightTrans.position.y + Map.halfEdgeLengthY_block)) * forwardDirection_parallelToGround;

                Vector3 leftRayDirection = Quaternion.AngleAxis(90f, forwardDirection) * backRayDirection;
                Vector3 leftRayEnd = lightTrans.position + leftRayDirection * lightComponent.range;
                //计算cuttingEdgeInGround_left
                if (leftRayEnd.y > Map.halfEdgeLengthY_block)
                    cuttingEdgeInGround_left = lightTrans.position + (lightTrans.position.y - Map.halfEdgeLengthY_block) / (lightTrans.position.y - leftRayEnd.y) * lightComponent.range * leftRayDirection;
                else if (leftRayEnd.y > -Map.halfEdgeLengthY_block)
                    cuttingEdgeInGround_left = leftRayEnd;
                else
                {
                    //中心光线通过光锥“平底”的点
                    Vector3 circleCenterPos = lightTrans.position + Mathf.Cos(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad) * lightComponent.range * forwardDirection;
                    //cuttingEdgeInGround_left与circleCenterPos的高度差可知，与circleCenterPos的距离可知，求 其与circleCenterPos的连线 与 circleCenterPos与leftRayEnd的连线（平底圆形水平半径） 的夹角
                    float distancePerpendicular = (-Map.halfEdgeLengthY_block - circleCenterPos.y) / Vector3.Cross(forwardDirection, rightDirection_parallelToGround).y;
                    float circleRadius = Mathf.Sin(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad) * lightComponent.range;

                    float divideValue = distancePerpendicular / circleRadius;
                    float angleRad;
                    if (divideValue > 1f)
                    {
                        Debug.Log("Error! divideValue > 1f! distancePerpendicular == " + distancePerpendicular + "; Vector3.Cross(forwardDirection, rightDirection_parallelToGround).y == " + Vector3.Cross(forwardDirection, rightDirection_parallelToGround).y);
                        angleRad = Mathf.PI * 0.5f;
                    }
                    else if (divideValue < -1f)
                    {
                        Debug.Log("Error! divideValue < -1f! distancePerpendicular == " + distancePerpendicular + "; Vector3.Cross(forwardDirection, rightDirection_parallelToGround).y == " + Vector3.Cross(forwardDirection, rightDirection_parallelToGround).y);
                        angleRad = Mathf.PI * 0.5f;
                    }
                    else
                        angleRad = Mathf.Asin(divideValue);

                    //将 由circleCenterPos指向leftRayEnd的向量 沿着forward轴逆时针旋转angle角度，获得cuttingEdgeInGround_left
                    cuttingEdgeInGround_left = circleCenterPos + Quaternion.AngleAxis(-angleRad * Mathf.Rad2Deg, forwardDirection) * (leftRayEnd - circleCenterPos);
                }
            }
            else//光源在ground内部
            {
                Vector3 backRayDirection = Quaternion.AngleAxis(lightComponent.spotAngle * 0.5f, rightDirection_parallelToGround) * forwardDirection;
                Vector3 backRayEnd = lightTrans.position + backRayDirection * lightComponent.range;
                //计算cuttingEdgeInGround_back，待用
                if (backRayEnd.y < -Map.halfEdgeLengthY_block)
                    cuttingEdgeInGround_back = lightTrans.position + (lightTrans.position.y + Map.halfEdgeLengthY_block) / (lightTrans.position.y - backRayEnd.y) * lightComponent.range * backRayDirection;
                else if (backRayEnd.y > Map.halfEdgeLengthY_block)
                    cuttingEdgeInGround_back = lightTrans.position + (lightTrans.position.y - Map.halfEdgeLengthY_block) / (lightTrans.position.y - backRayEnd.y) * lightComponent.range * backRayDirection;
                else
                    cuttingEdgeInGround_back = backRayEnd;

                Vector3 frontRayDirection = Quaternion.AngleAxis(-lightComponent.spotAngle * 0.5f, rightDirection_parallelToGround) * forwardDirection;
                Vector3 frontRayEnd = lightTrans.position + frontRayDirection * lightComponent.range;
                //计算cuttingEdgeInGround_front，待用
                if (frontRayEnd.y < -Map.halfEdgeLengthY_block)
                    cuttingEdgeInGround_front = lightTrans.position + (lightTrans.position.y + Map.halfEdgeLengthY_block) / (lightTrans.position.y - frontRayEnd.y) * lightComponent.range * frontRayDirection;
                else if (frontRayEnd.y > Map.halfEdgeLengthY_block)
                    cuttingEdgeInGround_front = lightTrans.position + (lightTrans.position.y - Map.halfEdgeLengthY_block) / (lightTrans.position.y - frontRayEnd.y) * lightComponent.range * frontRayDirection;
                else
                    cuttingEdgeInGround_front = frontRayEnd;

                float dotBack = Vector3.Dot(forwardDirection_parallelToGround, cuttingEdgeInGround_back);
                float dotFront = Vector3.Dot(forwardDirection_parallelToGround, cuttingEdgeInGround_front);
                if (dotBack > 0f == dotFront > 0f)//back与front的水平分量同向
                {
                    if (dotBack > dotFront)
                        cuttingEdgeInGround_front = lightTrans.position;
                    else
                        cuttingEdgeInGround_back = lightTrans.position;
                }

                Vector3 leftRayDirection = Quaternion.AngleAxis(90f, forwardDirection) * backRayDirection;
                Vector3 leftRayEnd = lightTrans.position + leftRayDirection * lightComponent.range;
                //计算cuttingEdgeInGround_left
                if (leftRayEnd.y < -Map.halfEdgeLengthY_block)
                    cuttingEdgeInGround_left = lightTrans.position + (lightTrans.position.y + Map.halfEdgeLengthY_block) / (lightTrans.position.y - leftRayEnd.y) * lightComponent.range * leftRayDirection;
                else if (leftRayEnd.y > Map.halfEdgeLengthY_block)
                    cuttingEdgeInGround_left = lightTrans.position + (lightTrans.position.y - Map.halfEdgeLengthY_block) / (lightTrans.position.y - leftRayEnd.y) * lightComponent.range * leftRayDirection;
                else
                    cuttingEdgeInGround_left = leftRayEnd;
            }

            Vector3 cuttingEdgeInGround_left_leveled = new(cuttingEdgeInGround_left.x, lightTrans.position.y, cuttingEdgeInGround_left.z);
            Vector3 cuttingEdgeInGround_right = (lightTrans.position + Vector3.Dot(cuttingEdgeInGround_left_leveled - lightTrans.position, forwardDirection_parallelToGround) * forwardDirection_parallelToGround) * 2 - cuttingEdgeInGround_left_leveled;
            cuttingEdgeInGround_right.y = cuttingEdgeInGround_left.y;

            /*初步划定探测范围，严谨性待验证*/
            float scanCenterX = (cuttingEdgeInGround_back.x + cuttingEdgeInGround_front.x + cuttingEdgeInGround_left.x + cuttingEdgeInGround_right.x) * 0.25f;
            float scanCenterZ = (cuttingEdgeInGround_back.z + cuttingEdgeInGround_front.z + cuttingEdgeInGround_left.z + cuttingEdgeInGround_right.z) * 0.25f;

            float scanRadiusSquared = (scanCenterX - cuttingEdgeInGround_front.x) * (scanCenterX - cuttingEdgeInGround_front.x) + (scanCenterZ - cuttingEdgeInGround_front.z) * (scanCenterZ - cuttingEdgeInGround_front.z);
            float scanRadiusSquared_potential = (scanCenterX - cuttingEdgeInGround_back.x) * (scanCenterX - cuttingEdgeInGround_back.x) + (scanCenterZ - cuttingEdgeInGround_back.z) * (scanCenterZ - cuttingEdgeInGround_back.z);
            if (scanRadiusSquared_potential > scanRadiusSquared)
                scanRadiusSquared = scanRadiusSquared_potential;
            scanRadiusSquared_potential = (scanCenterX - cuttingEdgeInGround_left.x) * (scanCenterX - cuttingEdgeInGround_left.x) + (scanCenterZ - cuttingEdgeInGround_left.z) * (scanCenterZ - cuttingEdgeInGround_left.z);
            if (scanRadiusSquared_potential > scanRadiusSquared)
                scanRadiusSquared = scanRadiusSquared_potential;
            float scanRadius = Mathf.Sqrt(scanRadiusSquared);

            leftLimit = Mathf.FloorToInt((scanCenterX - scanRadius) * Map.reciprocalEdgeLengthX_block);
            rightLimit = Mathf.FloorToInt((scanCenterX + scanRadius) * Map.reciprocalEdgeLengthX_block);
            downLimit = Mathf.FloorToInt((scanCenterZ - scanRadius) * Map.reciprocalEdgeLengthZ_block);
            upLimit = Mathf.FloorToInt((scanCenterZ + scanRadius) * Map.reciprocalEdgeLengthZ_block);

        }

        /*精确计算覆盖范围，检测遮蔽，部署Block*/
        for (int column = leftLimit; column <= rightLimit; column += 1)
        {
            for (int row = downLimit; row <= upLimit; row += 1)
            {
                Vector3 blockPos = new(column * Map.edgeLengthX_block + Map.halfEdgeLengthX_block, 0f, row * Map.edgeLengthZ_block + Map.halfEdgeLengthZ_block);

                //粗筛，将block简化为球体，不考虑lightRange，判断block是否可能在光锥内
                Vector3 vector_lightOriginToBlockCenter = blockPos - lightTrans.position;
                float angle_blockVector_lightDirection = Vector3.Angle(forwardDirection, vector_lightOriginToBlockCenter);
                if (angle_blockVector_lightDirection > lightComponent.spotAngle * 0.5f)
                {
                    if (angle_blockVector_lightDirection - lightComponent.spotAngle * 0.5f < 90f)
                    {
                        float sinAngle = Mathf.Sin((angle_blockVector_lightDirection - lightComponent.spotAngle * 0.5f) * Mathf.Deg2Rad);
                        if (vector_lightOriginToBlockCenter.sqrMagnitude * sinAngle * sinAngle > Map.sphereRadiusSquared_block)
                            continue;
                    }
                    else
                    {
                        if (vector_lightOriginToBlockCenter.sqrMagnitude > Map.sphereRadiusSquared_block)
                            continue;
                    }
                }

                //判断光源相对于block的位置，判断是否在无遮挡条件下被光源照射，若是，设定LineCast检测参数
                SurfaceScanData surfaceScanData = SurfaceScanData_AsSphereLamp(column, row);
                if (surfaceScanData.surfacesToScan == -1)
                    continue;

                //对surfaceBlock做点阵扫描，计算每点是否在光源照射范围内，若是，做障碍检测
                if ((surfaceScanData.enterPoint - lightTrans.position).sqrMagnitude <= lightComponent.range * lightComponent.range
                    && Vector3.Dot(surfaceScanData.enterPoint - lightTrans.position, forwardDirection) * Vector3.Dot(surfaceScanData.enterPoint - lightTrans.position, forwardDirection) >= (surfaceScanData.enterPoint - lightTrans.position).sqrMagnitude * Mathf.Cos(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad) * Mathf.Cos(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad)
                    && Physics.Linecast(lightTrans.position, surfaceScanData.enterPoint, 7) == false)
                    goto DeployBlock;

                if (surfaceScanData.surfacesToScan == 3)
                {
                    //两个辅轴从stepsB==1,setpsC==1开始交错扫描
                    for (int stepsB = 1; stepsB <= surfaceScanData.stepsB; stepsB += 1)
                    {
                        for (int stepsC = 1; stepsC <= surfaceScanData.stepsC; stepsC += 1)
                        {
                            Vector3 pos = surfaceScanData.startVertex + stepsB * surfaceScanData.spacingB * surfaceScanData.directionB + stepsC * surfaceScanData.spacingC * surfaceScanData.directionC;
                            if ((pos - lightTrans.position).sqrMagnitude <= lightComponent.range * lightComponent.range
                                && Vector3.Dot(pos - lightTrans.position, forwardDirection) * Vector3.Dot(pos - lightTrans.position, forwardDirection) >= (pos - lightTrans.position).sqrMagnitude * Mathf.Cos(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad) * Mathf.Cos(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad)
                                && Physics.Linecast(lightTrans.position, pos, 7) == false)
                                goto DeployBlock;
                        }
                    }
                    //主轴与第一辅助轴从stepsA==0,stepsB==0开始交错扫描，主轴与第二辅助轴从stepsA==0,stepsC==1开始交错扫描
                    for (int stepsA = 0; stepsA <= surfaceScanData.stepsA; stepsA += 1)
                    {
                        for (int stepsB = 0; stepsB <= surfaceScanData.stepsB; stepsB += 1)
                        {
                            Vector3 pos = surfaceScanData.startVertex + stepsA * surfaceScanData.spacingA * surfaceScanData.directionA + stepsB * surfaceScanData.spacingB * surfaceScanData.directionB;
                            if ((pos - lightTrans.position).sqrMagnitude <= lightComponent.range * lightComponent.range
                                && Vector3.Dot(pos - lightTrans.position, forwardDirection) * Vector3.Dot(pos - lightTrans.position, forwardDirection) >= (pos - lightTrans.position).sqrMagnitude * Mathf.Cos(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad) * Mathf.Cos(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad)
                                && Physics.Linecast(lightTrans.position, pos, 7) == false)
                                goto DeployBlock;
                        }
                        for (int stepsC = 1; stepsC <= surfaceScanData.stepsC; stepsC += 1)
                        {
                            Vector3 pos = surfaceScanData.startVertex + stepsA * surfaceScanData.spacingA * surfaceScanData.directionA + stepsC * surfaceScanData.spacingC * surfaceScanData.directionC;
                            if ((pos - lightTrans.position).sqrMagnitude <= lightComponent.range * lightComponent.range
                                && Vector3.Dot(pos - lightTrans.position, forwardDirection) * Vector3.Dot(pos - lightTrans.position, forwardDirection) >= (pos - lightTrans.position).sqrMagnitude * Mathf.Cos(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad) * Mathf.Cos(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad)
                                && Physics.Linecast(lightTrans.position, pos, 7) == false)
                                goto DeployBlock;
                        }
                    }
                }
                else if (surfaceScanData.surfacesToScan == 2)
                {
                    //主轴与第一辅助轴从stepsA == 0,stepsB == 0开始交错扫描，主轴与第二辅助轴从stepsA == 0,stepsC == 1开始交错扫描
                    for (int stepsA = 0; stepsA <= surfaceScanData.stepsA; stepsA += 1)
                    {
                        for (int stepsB = 0; stepsB <= surfaceScanData.stepsB; stepsB += 1)
                        {
                            Vector3 pos = surfaceScanData.startVertex + stepsA * surfaceScanData.spacingA * surfaceScanData.directionA + stepsB * surfaceScanData.spacingB * surfaceScanData.directionB;
                            if ((pos - lightTrans.position).sqrMagnitude <= lightComponent.range * lightComponent.range
                                && Vector3.Dot(pos - lightTrans.position, forwardDirection) * Vector3.Dot(pos - lightTrans.position, forwardDirection) >= (pos - lightTrans.position).sqrMagnitude * Mathf.Cos(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad) * Mathf.Cos(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad)
                                && Physics.Linecast(lightTrans.position, pos, 7) == false)
                                goto DeployBlock;
                        }
                        for (int stepsC = 1; stepsC <= surfaceScanData.stepsC; stepsC += 1)
                        {
                            Vector3 pos = surfaceScanData.startVertex + stepsA * surfaceScanData.spacingA * surfaceScanData.directionA + stepsC * surfaceScanData.spacingC * surfaceScanData.directionC;
                            if ((pos - lightTrans.position).sqrMagnitude <= lightComponent.range * lightComponent.range
                                && Vector3.Dot(pos - lightTrans.position, forwardDirection) * Vector3.Dot(pos - lightTrans.position, forwardDirection) >= (pos - lightTrans.position).sqrMagnitude * Mathf.Cos(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad) * Mathf.Cos(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad)
                                && Physics.Linecast(lightTrans.position, pos, 7) == false)
                                goto DeployBlock;
                        }
                    }
                }
                else if (surfaceScanData.surfacesToScan == 1)
                {
                    //主轴与第一辅助轴从stepsA == 0,stepsB == 0开始交错扫描
                    for (int stepsA = 0; stepsA <= surfaceScanData.stepsA; stepsA += 1)
                    {
                        for (int stepsB = 0; stepsB <= surfaceScanData.stepsB; stepsB += 1)
                        {
                            Vector3 pos = surfaceScanData.startVertex + stepsA * surfaceScanData.spacingA * surfaceScanData.directionA + stepsB * surfaceScanData.spacingB * surfaceScanData.directionB;
                            if ((pos - lightTrans.position).sqrMagnitude <= lightComponent.range * lightComponent.range
                                && Vector3.Dot(pos - lightTrans.position, forwardDirection) * Vector3.Dot(pos - lightTrans.position, forwardDirection) >= (pos - lightTrans.position).sqrMagnitude * Mathf.Cos(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad) * Mathf.Cos(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad)
                                && Physics.Linecast(lightTrans.position, pos, 7) == false)
                                goto DeployBlock;
                        }
                    }
                }
                else//surfaceScanData.surfacesToScan == 0
                    goto DeployBlock;

                //若未跳转，即未被照射，进入下一个block迭代
                continue;

            DeployBlock:

                /*部署block*/
                DeployBlock(Map.blocks[column][row]);
            }
        }

        /*清理失去照射的block*/
        UpdateUncoveredBlocks();

        /*用新集合取代旧集合*/
        blocksCoveredByThis_previous.Clear();
        (blocksCoveredByThis_now, blocksCoveredByThis_previous) = (blocksCoveredByThis_previous, blocksCoveredByThis_now);

    }
}


public class AndroidWithLamp : Lamp
{
    public CharacterController androidCharacterController;
    public Transform androidTransform;

    public AndroidWithLamp(GameObject lampObject, float solidProbability, int brickSource) : base(lampObject, solidProbability, brickSource)
    {
        androidCharacterController = lampObject.GetComponent<CharacterController>();
        androidTransform = lampObject.transform;
    }
}