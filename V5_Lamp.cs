using System.Collections.Generic;
using UnityEngine;

public struct SurfaceScanData
{
    public Vector3 enterPoint;
    /* 
     * ��surfacesToScan==3�����������stepsB==1,setpsC==1��ʼ����ɨ�裬�������һ�������stepsA==0,stepsB==0��ʼ����ɨ�裬������ڶ��������stepsA==0,stepsB==1��ʼ����ɨ��
     * ��surfacesToScan==2���������һ�������stepsA==0,stepsB==0��ʼ����ɨ�裬������ڶ��������stepsA==0,stepsB==1��ʼ����ɨ��
     * ��surfacesToScan==1�������һ�������stepsA==0,stepsB==0��ʼ����ɨ��
     * ��surfacesToScan==0��ֱ�ӽ��벿��
     * ��surfacesToScan==-1, ������ɨ��
     */
    public int surfacesToScan;
    public Vector3 startVertex;
    public Vector3 directionA;//��ɨ�跽��
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
    /*��ĳ�Ա����*/
    public Transform lightTrans;//"__Light"�������transform
    public Light lightComponent;

    public List<Block> blocksCoveredByThis_previous;
    public List<Block> blocksCoveredByThis_now;

    public float solidProbability;
    public int brickSource;

    public DelegatedLampMethod UpdateGroundBlockCoverage;

    /*���캯��*/
    public Lamp(GameObject lampObject, float solidProbability, int brickSource)//brickSource==-1��ExNBricksPool;else, prefabRelicBricks[brickSource]
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
            Debug.Log("��ȡ��Դ���ʹ��� ��Դ������λ��Ϊ��" + lampObject.transform.position);

    }

    public SurfaceScanData SurfaceScanData_AsSphereLamp(int column, int row)
    {
        //����LineCast������������ֵ
        SurfaceScanData surfaceScanData;

        //�жϹ�Դ�����block��λ�ã��ж��Ƿ������ڵ������±���Դ���䣬���ǣ��趨LineCast������
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
                    surfaceScanData.enterPoint = lightTrans.position;//LineCast�����յ���ͬ

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

        //�ж��Ƿ��ھɸ��Ƿ�Χ��
        int indexInPreviousCoverage = blocksCoveredByThis_previous.IndexOf(thisBlock);
        if (indexInPreviousCoverage != -1)//�ھɸ��Ƿ�Χ�ڣ���blocksCoveredByThis_previous�г��루���б��������µ�block����ʧȥ���䡢��Ҫ�����block��
        {
            blocksCoveredByThis_previous[indexInPreviousCoverage] = blocksCoveredByThis_previous[^1];
            blocksCoveredByThis_previous.RemoveAt(blocksCoveredByThis_previous.Count - 1);
        }
        else//���ھɸ��Ƿ�Χ��
        {
            thisBlock.num_lampsCoveringThis += 1;
            if (thisBlock.num_lampsCoveringThis == 1 && solidProbability != 0f && thisBlock.brick == null)//�������һ���ж����������ĸߣ�
            {
                if (solidProbability == 1f || UnityEngine.Random.Range(0f, 1f) <= solidProbability)
                {
                    if (brickSource == -1)
                    {
                        thisBlock.brick = Pool.exnBricks[^1];//�ɻ����ȷ�����ò�Ϊnull
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

            if (thisBlock.num_lampsCoveringThis == 0 && thisBlock.brick != null)//��blockδ���κι�Դ�����Ҳ�Ϊ��գ�������brick
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
        /*�ų���Դ���߻���������ڲ���������������������Լ��㸲�Ƿ�Χ*/
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

        /*��ȷ���㸲�Ƿ�Χ��ΪLineCast�趨����������ڱΣ�����Block*/
        for (int column = leftLimit; column <= rightLimit; column += 1)
        {
            for (int row = downLimit; row <= upLimit; row += 1)
            {
                //�жϹ�Դ�����block��λ�ã��ж��Ƿ������ڵ������±���Դ���䣬���ǣ��趨LineCast������
                SurfaceScanData surfaceScanData = SurfaceScanData_AsSphereLamp(column, row);
                if (surfaceScanData.surfacesToScan == -1)
                    continue;

                //�����ڵ������±���Դ�����block���ϰ�����
                if (Physics.Linecast(lightTrans.position, surfaceScanData.enterPoint, 7) == false)
                    goto DeployBlock;

                if (surfaceScanData.surfacesToScan == 3)
                {
                    //���������stepsB==1,setpsC==1��ʼ����ɨ��
                    for (int stepsB = 1; stepsB <= surfaceScanData.stepsB; stepsB += 1)
                    {
                        for (int stepsC = 1; stepsC <= surfaceScanData.stepsC; stepsC += 1)
                        {
                            Vector3 pos = surfaceScanData.startVertex + stepsB * surfaceScanData.spacingB * surfaceScanData.directionB + stepsC * surfaceScanData.spacingC * surfaceScanData.directionC;
                            if ((lightTrans.position - pos).sqrMagnitude <= lightComponent.range * lightComponent.range && Physics.Linecast(lightTrans.position, pos, 7) == false)
                                goto DeployBlock;
                        }
                    }
                    //�������һ�������stepsA==0,stepsB==0��ʼ����ɨ�裬������ڶ��������stepsA==0,stepsB==1��ʼ����ɨ��
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
                    //�������һ�������stepsA == 0,stepsB == 0��ʼ����ɨ�裬������ڶ��������stepsA == 0,stepsB == 1��ʼ����ɨ��
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
                    //�����һ�������stepsA == 0,stepsB == 0��ʼ����ɨ��
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

                //��δ��ת����δ�����䣬������һ��block����
                continue;

            DeployBlock:

                /*����block*/
                DeployBlock(Map.blocks[column][row]);
            }
        }

        /*����ʧȥ�����block*/
        UpdateUncoveredBlocks();

        /*���¼���ȡ���ɼ���*/
        blocksCoveredByThis_previous.Clear();
        (blocksCoveredByThis_now, blocksCoveredByThis_previous) = (blocksCoveredByThis_previous, blocksCoveredByThis_now);
    }
    public void UpdateGroundBlockCoverage_Spot()
    {
        /*
         * ���Լ��㸲�Ƿ�Χ
         */

        //��������ֵ
        int leftLimit, rightLimit, downLimit, upLimit;

        //��ֵ����
        Vector3 forwardDirection = lightTrans.forward;

        //back,front,right,left������forwardDirection_parallelToGround����
        Vector3 forwardDirection_parallelToGround = new Vector3(forwardDirection.x, 0f, forwardDirection.z).normalized;

        if (forwardDirection_parallelToGround == Vector3.zero)//��ֱ�������䣬��ΪԲ��
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
        else//�Ǵ�ֱ�������
        {
            Vector3 cuttingEdgeInGround_front, cuttingEdgeInGround_back;//back��front���Գ�
            Vector3 cuttingEdgeInGround_left;//right��left�Գƣ�ֻ�����һ��

            Vector3 rightDirection_parallelToGround = Vector3.Cross(Vector3.up, forwardDirection_parallelToGround);

            if (lightTrans.position.y > Map.halfEdgeLengthY_block)//��Դ��ground�Ϸ�
            {
                Vector3 backRayDirection = Quaternion.AngleAxis(lightComponent.spotAngle * 0.5f, rightDirection_parallelToGround) * forwardDirection;
                Vector3 backRayEnd = lightTrans.position + backRayDirection * lightComponent.range;
                //����cuttingEdgeInGround_back
                if (Vector3.Dot(backRayDirection, forwardDirection_parallelToGround) > 0f)//back��ǰ
                {
                    if (backRayEnd.y <= Map.halfEdgeLengthY_block)
                        cuttingEdgeInGround_back = lightTrans.position + (lightTrans.position.y - Map.halfEdgeLengthY_block) / (lightTrans.position.y - backRayEnd.y) * lightComponent.range * backRayDirection;//��������������
                    else
                        return;
                }
                else//back�ں�����·�
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
                //����cuttingEdgeInGround_front
                if (frontRayEnd.y < -Map.halfEdgeLengthY_block)
                    cuttingEdgeInGround_front = lightTrans.position + (lightTrans.position.y + Map.halfEdgeLengthY_block) / (lightTrans.position.y - frontRayEnd.y) * lightComponent.range * frontRayDirection;
                else if (frontRayEnd.y < Map.halfEdgeLengthY_block)
                    cuttingEdgeInGround_front = frontRayEnd;
                else
                    cuttingEdgeInGround_front = new Vector3(lightTrans.position.x, Map.halfEdgeLengthY_block, lightTrans.position.z) + Mathf.Sqrt(lightComponent.range * lightComponent.range - (lightTrans.position.y - Map.halfEdgeLengthY_block) * (lightTrans.position.y - Map.halfEdgeLengthY_block)) * forwardDirection_parallelToGround;

                Vector3 leftRayDirection = Quaternion.AngleAxis(90f, forwardDirection) * backRayDirection;
                Vector3 leftRayEnd = lightTrans.position + leftRayDirection * lightComponent.range;
                //����cuttingEdgeInGround_left
                if (leftRayEnd.y < -Map.halfEdgeLengthY_block)
                    cuttingEdgeInGround_left = lightTrans.position + (lightTrans.position.y + Map.halfEdgeLengthY_block) / (lightTrans.position.y - leftRayEnd.y) * lightComponent.range * leftRayDirection;
                else if (leftRayEnd.y < Map.halfEdgeLengthY_block)
                    cuttingEdgeInGround_left = leftRayEnd;
                else
                {
                    //���Ĺ���ͨ����׶��ƽ�ס��ĵ�
                    Vector3 circleCenterPos = lightTrans.position + Mathf.Cos(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad) * lightComponent.range * forwardDirection;
                    //cuttingEdgeInGround_left�� circleCenterPos��leftRayEnd������ �Ĵ��߾����֪����circleCenterPos�ľ����֪���� ����circleCenterPos������ �� circleCenterPos��leftRayEnd�����ߣ�ƽ��Բ��ˮƽ�뾶�� �ļн�
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

                    //�� ��circleCenterPosָ��leftRayEnd������ ����forward��˳ʱ����תangle�Ƕȣ����cuttingEdgeInGround_left
                    cuttingEdgeInGround_left = circleCenterPos + Quaternion.AngleAxis(angleRad * Mathf.Rad2Deg, forwardDirection) * (leftRayEnd - circleCenterPos);
                }
            }
            else if (lightTrans.position.y < -Map.halfEdgeLengthY_block)//��Դ��ground�·�
            {
                Vector3 backRayDirection = Quaternion.AngleAxis(-lightComponent.spotAngle * 0.5f, rightDirection_parallelToGround) * forwardDirection;
                Vector3 backRayEnd = lightTrans.position + backRayDirection * lightComponent.range;
                //����cuttingEdgeInGround_back
                if (Vector3.Dot(backRayDirection, forwardDirection_parallelToGround) > 0f)//back��ǰ
                {
                    if (backRayEnd.y >= -Map.halfEdgeLengthY_block)
                        cuttingEdgeInGround_back = lightTrans.position + (lightTrans.position.y + Map.halfEdgeLengthY_block) / (lightTrans.position.y - backRayEnd.y) * lightComponent.range * backRayDirection;//��������������
                    else
                        return;
                }
                else//back�ں�����·�
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
                //����cuttingEdgeInGround_front
                if (frontRayEnd.y > Map.halfEdgeLengthY_block)
                    cuttingEdgeInGround_front = lightTrans.position + (lightTrans.position.y - Map.halfEdgeLengthY_block) / (lightTrans.position.y - frontRayEnd.y) * lightComponent.range * frontRayDirection;
                else if (frontRayEnd.y > -Map.halfEdgeLengthY_block)
                    cuttingEdgeInGround_front = frontRayEnd;
                else
                    cuttingEdgeInGround_front = new Vector3(lightTrans.position.x, -Map.halfEdgeLengthY_block, lightTrans.position.z) + Mathf.Sqrt(lightComponent.range * lightComponent.range - (lightTrans.position.y + Map.halfEdgeLengthY_block) * (lightTrans.position.y + Map.halfEdgeLengthY_block)) * forwardDirection_parallelToGround;

                Vector3 leftRayDirection = Quaternion.AngleAxis(90f, forwardDirection) * backRayDirection;
                Vector3 leftRayEnd = lightTrans.position + leftRayDirection * lightComponent.range;
                //����cuttingEdgeInGround_left
                if (leftRayEnd.y > Map.halfEdgeLengthY_block)
                    cuttingEdgeInGround_left = lightTrans.position + (lightTrans.position.y - Map.halfEdgeLengthY_block) / (lightTrans.position.y - leftRayEnd.y) * lightComponent.range * leftRayDirection;
                else if (leftRayEnd.y > -Map.halfEdgeLengthY_block)
                    cuttingEdgeInGround_left = leftRayEnd;
                else
                {
                    //���Ĺ���ͨ����׶��ƽ�ס��ĵ�
                    Vector3 circleCenterPos = lightTrans.position + Mathf.Cos(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad) * lightComponent.range * forwardDirection;
                    //cuttingEdgeInGround_left��circleCenterPos�ĸ߶Ȳ��֪����circleCenterPos�ľ����֪���� ����circleCenterPos������ �� circleCenterPos��leftRayEnd�����ߣ�ƽ��Բ��ˮƽ�뾶�� �ļн�
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

                    //�� ��circleCenterPosָ��leftRayEnd������ ����forward����ʱ����תangle�Ƕȣ����cuttingEdgeInGround_left
                    cuttingEdgeInGround_left = circleCenterPos + Quaternion.AngleAxis(-angleRad * Mathf.Rad2Deg, forwardDirection) * (leftRayEnd - circleCenterPos);
                }
            }
            else//��Դ��ground�ڲ�
            {
                Vector3 backRayDirection = Quaternion.AngleAxis(lightComponent.spotAngle * 0.5f, rightDirection_parallelToGround) * forwardDirection;
                Vector3 backRayEnd = lightTrans.position + backRayDirection * lightComponent.range;
                //����cuttingEdgeInGround_back������
                if (backRayEnd.y < -Map.halfEdgeLengthY_block)
                    cuttingEdgeInGround_back = lightTrans.position + (lightTrans.position.y + Map.halfEdgeLengthY_block) / (lightTrans.position.y - backRayEnd.y) * lightComponent.range * backRayDirection;
                else if (backRayEnd.y > Map.halfEdgeLengthY_block)
                    cuttingEdgeInGround_back = lightTrans.position + (lightTrans.position.y - Map.halfEdgeLengthY_block) / (lightTrans.position.y - backRayEnd.y) * lightComponent.range * backRayDirection;
                else
                    cuttingEdgeInGround_back = backRayEnd;

                Vector3 frontRayDirection = Quaternion.AngleAxis(-lightComponent.spotAngle * 0.5f, rightDirection_parallelToGround) * forwardDirection;
                Vector3 frontRayEnd = lightTrans.position + frontRayDirection * lightComponent.range;
                //����cuttingEdgeInGround_front������
                if (frontRayEnd.y < -Map.halfEdgeLengthY_block)
                    cuttingEdgeInGround_front = lightTrans.position + (lightTrans.position.y + Map.halfEdgeLengthY_block) / (lightTrans.position.y - frontRayEnd.y) * lightComponent.range * frontRayDirection;
                else if (frontRayEnd.y > Map.halfEdgeLengthY_block)
                    cuttingEdgeInGround_front = lightTrans.position + (lightTrans.position.y - Map.halfEdgeLengthY_block) / (lightTrans.position.y - frontRayEnd.y) * lightComponent.range * frontRayDirection;
                else
                    cuttingEdgeInGround_front = frontRayEnd;

                float dotBack = Vector3.Dot(forwardDirection_parallelToGround, cuttingEdgeInGround_back);
                float dotFront = Vector3.Dot(forwardDirection_parallelToGround, cuttingEdgeInGround_front);
                if (dotBack > 0f == dotFront > 0f)//back��front��ˮƽ����ͬ��
                {
                    if (dotBack > dotFront)
                        cuttingEdgeInGround_front = lightTrans.position;
                    else
                        cuttingEdgeInGround_back = lightTrans.position;
                }

                Vector3 leftRayDirection = Quaternion.AngleAxis(90f, forwardDirection) * backRayDirection;
                Vector3 leftRayEnd = lightTrans.position + leftRayDirection * lightComponent.range;
                //����cuttingEdgeInGround_left
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

            /*��������̽�ⷶΧ���Ͻ��Դ���֤*/
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

        /*��ȷ���㸲�Ƿ�Χ������ڱΣ�����Block*/
        for (int column = leftLimit; column <= rightLimit; column += 1)
        {
            for (int row = downLimit; row <= upLimit; row += 1)
            {
                Vector3 blockPos = new(column * Map.edgeLengthX_block + Map.halfEdgeLengthX_block, 0f, row * Map.edgeLengthZ_block + Map.halfEdgeLengthZ_block);

                //��ɸ����block��Ϊ���壬������lightRange���ж�block�Ƿ�����ڹ�׶��
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

                //�жϹ�Դ�����block��λ�ã��ж��Ƿ������ڵ������±���Դ���䣬���ǣ��趨LineCast������
                SurfaceScanData surfaceScanData = SurfaceScanData_AsSphereLamp(column, row);
                if (surfaceScanData.surfacesToScan == -1)
                    continue;

                //��surfaceBlock������ɨ�裬����ÿ���Ƿ��ڹ�Դ���䷶Χ�ڣ����ǣ����ϰ����
                if ((surfaceScanData.enterPoint - lightTrans.position).sqrMagnitude <= lightComponent.range * lightComponent.range
                    && Vector3.Dot(surfaceScanData.enterPoint - lightTrans.position, forwardDirection) * Vector3.Dot(surfaceScanData.enterPoint - lightTrans.position, forwardDirection) >= (surfaceScanData.enterPoint - lightTrans.position).sqrMagnitude * Mathf.Cos(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad) * Mathf.Cos(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad)
                    && Physics.Linecast(lightTrans.position, surfaceScanData.enterPoint, 7) == false)
                    goto DeployBlock;

                if (surfaceScanData.surfacesToScan == 3)
                {
                    //���������stepsB==1,setpsC==1��ʼ����ɨ��
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
                    //�������һ�������stepsA==0,stepsB==0��ʼ����ɨ�裬������ڶ��������stepsA==0,stepsC==1��ʼ����ɨ��
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
                    //�������һ�������stepsA == 0,stepsB == 0��ʼ����ɨ�裬������ڶ��������stepsA == 0,stepsC == 1��ʼ����ɨ��
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
                    //�������һ�������stepsA == 0,stepsB == 0��ʼ����ɨ��
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

                //��δ��ת����δ�����䣬������һ��block����
                continue;

            DeployBlock:

                /*����block*/
                DeployBlock(Map.blocks[column][row]);
            }
        }

        /*����ʧȥ�����block*/
        UpdateUncoveredBlocks();

        /*���¼���ȡ���ɼ���*/
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