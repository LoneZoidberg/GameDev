using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*声明委托*/
public delegate void Delegate_Lamp_SetRayDotsLocalPosition();
public delegate void Delegate_Lamp_PredictGroundBlockCoverage();

public class Lamp
{
    /*类的变量*/
    public readonly Transform lightTrans;//"__Light"子物体的transform
    public readonly Light lightComponent;
    public List<Block> blocksCoveredByThis;
    public List<Block> blocksCoveredByThis_predicted;
    public float solidProbability;
    public Vector3[][] rayDotsLocalPositon;
    public Delegate_Lamp_SetRayDotsLocalPosition SetRayDotsLocalPosition; //如果光源大小不动态变化，无需使用此变量
    public Delegate_Lamp_PredictGroundBlockCoverage PredictGroundBlockCoverage;

    /*构造函数*/
    public Lamp(GameObject lampObject, float solidProbability)
    {
        lightTrans = lampObject.transform.Find("__Light");
        lightComponent = lightTrans.GetComponent<Light>();
        lightComponent.intensity = lightComponent.range * Map.ratio_lightIntensityToRange;
        blocksCoveredByThis = new();
        blocksCoveredByThis_predicted = new();
        this.solidProbability = solidProbability;

        LightType lightType = lightComponent.type;
        if (lightType == LightType.Point)
        {
            SetRayDotsLocalPosition = SetRayDotsLocalPosition_Sphere;
            PredictGroundBlockCoverage = PredictGroundBlockCoverage_Sphere;
        }
        else if (lightType == LightType.Spot)
        {
            SetRayDotsLocalPosition = SetRayDotsLocalPosition_Spot;
            PredictGroundBlockCoverage = PredictGroundBlockCoverage_Spot;
        }
    }

    /*通用方法*/
    void SetCover(Block block, bool parameter)
    {
        if (parameter == true)
        {
            block.num_lampsCoveringThis += 1;
            blocksCoveredByThis.Add(block);
        }
        else
        {
            block.num_lampsCoveringThis -= 1;
            blocksCoveredByThis.Remove(block);
        }
    }
    public void SetLightRange(float range)
    {
        lightComponent.range = range;
        lightComponent.intensity = range * Map.ratio_lightIntensityToRange;
    }
    public void ShowNum_RaysDots()
    {
        int numDots = 0;
        for (int r = 0; r < rayDotsLocalPositon.Length; r++)
        {
            numDots += rayDotsLocalPositon[r].Length;
        }
        UnityEngine.Debug.Log("LightRange为：" + lightComponent.range + ", Rays数量为：" + rayDotsLocalPositon.Length + ", Dots数量为：" + numDots);
    }
    public void UpdateGroundBlockCoverage()
    {
        /*清理失去当前lamp照射的block*/
        for (int i = blocksCoveredByThis.Count - 1; i >= 0; i -= 1)
        {
            Block thisBlock = blocksCoveredByThis[i];

            int indexInPrediction = blocksCoveredByThis_predicted.IndexOf(thisBlock);
            if (indexInPrediction == -1)//若block失去当前lamp照射
            {
                blocksCoveredByThis[i] = blocksCoveredByThis[^1];
                blocksCoveredByThis.RemoveAt(blocksCoveredByThis.Count - 1);
                thisBlock.num_lampsCoveringThis -= 1;
                if (thisBlock.num_lampsCoveringThis == 0 && thisBlock.brick != null)//若block未被任何光源照射且不为虚空，回收其brick
                {
                    if (thisBlock.brick.CompareTag("RelicBlockObject") == true)
                    {
                        thisBlock.brick.GetComponent<MeshRenderer>().material = MasterControl.material_exnBrick;
                        thisBlock.brick.tag = "ExNBrick";
                        thisBlock.brick.transform.parent = MasterControl.parent_exnBricks.transform;
                    }
                    thisBlock.brick.transform.position = MasterControl.FAR_AWAY;
                    MasterControl.pool_bricks.Add(thisBlock.brick);
                    thisBlock.brick = null;
                }
            }
            else//若block仍被当前lamp照射，则从 blocksCoveredByThis_predicted 中抽出，集合中最终剩下的就是新被照射的block
            {
                blocksCoveredByThis_predicted[indexInPrediction] = blocksCoveredByThis_predicted[^1];
                blocksCoveredByThis_predicted.RemoveAt(blocksCoveredByThis_predicted.Count - 1);
            }

        }

        /*部署新被照射的block*/
        foreach (Block thisBlock in blocksCoveredByThis_predicted)
        {
            if (thisBlock.num_lampsCoveringThis == 0)//若不被任何lamps照射，按概率部署brick
            {
                if (solidProbability == 1f)
                    goto DoDeploy;
                else if (solidProbability == 0f)
                    goto AfterDeploy;
                else
                {
                    if (UnityEngine.Random.Range(0f, 1f) <= solidProbability)
                        goto DoDeploy;
                    else
                        goto AfterDeploy;
                }
            DoDeploy:
                GameObject exnBrick = MasterControl.pool_bricks[^1];//由缓存池确保引用不为null
                MasterControl.pool_bricks.RemoveAt(MasterControl.pool_bricks.Count - 1);
                exnBrick.transform.position = thisBlock.CenterPos();
                thisBlock.brick = exnBrick;
            }
        AfterDeploy:
            SetCover(thisBlock, true);
        }
    }

    /*SphereLamp专用方法*/
    public void SetRayDotsLocalPosition_Sphere()
    {
        const int minTheta = -90;
        const int maxTheta = 90;
        const int minDeltaTheta = 18;
        const int minDeltaPhi = 6;
        const float deltaRange = Map.scanSpacing;

        /*纬线密度手动设定、有较高的下限，经线密度按需设定，故仅需为纬线（在经线方向上）补充点位*/
        List<List<Vector3>> _rayDotsLocalPositon = new();

        float maxRange = lightComponent.range;
        float minRange = maxRange > 0.5f ? 0.5f : maxRange * 0.5f;

        int deltaTheta;
        if (maxRange * Mathf.PI * 0.5f > deltaRange)
        {
            float analogDeltaTheta = (deltaRange / maxRange) * Mathf.Rad2Deg;
            deltaTheta = Mathf.FloorToInt(analogDeltaTheta);
            while (90 % deltaTheta != 0)
                deltaTheta += 1;
            if (deltaTheta < minDeltaTheta)
                deltaTheta = minDeltaTheta;
        }
        else
            deltaTheta = 90;

        //三层遍历，theta从minTheta到maxTheta，phi从0到360，range从minRange到maxRange
        int theta = minTheta;
        while (theta <= maxTheta)
        {
            int deltaPhi;
            if (theta != -90 && theta != 90)
            {
                float analogDeltaPhi = deltaRange / (maxRange * Mathf.Cos(theta * Mathf.Deg2Rad)) * Mathf.Rad2Deg;
                deltaPhi = Mathf.FloorToInt(analogDeltaPhi);
                if (deltaPhi < minDeltaPhi)
                    deltaPhi = minDeltaPhi;
                if (deltaPhi > 90)
                    deltaPhi = 90;
                while (90 % deltaPhi != 0)//确保x,-x,z,-z方向的探测精度
                    deltaPhi += 1;
            }
            else
                deltaPhi = 360;

            int phi = 0;
            while (phi < 360f)
            {
                List<Vector3> ray = new();
                float range = minRange;
                while (range <= maxRange)
                {
                    //ray线上点位计算
                    Vector3 pos = (Mathf.Sin(theta * Mathf.Deg2Rad) * Vector3.up + Mathf.Cos(theta * Mathf.Deg2Rad) * (Mathf.Sin(phi * Mathf.Deg2Rad) * Vector3.right + Mathf.Cos(phi * Mathf.Deg2Rad) * Vector3.forward)) * range;
                    ray.Add(pos);
                    //ray补充点位计算
                    float desiredDeltaTheta = deltaRange / range * Mathf.Rad2Deg;
                    if (deltaTheta > desiredDeltaTheta)
                    {
                        if (theta != -90 && theta != 90)
                        {
                            float deviationTheta = theta + deltaTheta * 0.5f - desiredDeltaTheta * 0.5f;
                            do
                            {
                                Vector3 fillPos = (Mathf.Sin(deviationTheta * Mathf.Deg2Rad) * Vector3.up + Mathf.Cos(deviationTheta * Mathf.Deg2Rad) * (Mathf.Sin(phi * Mathf.Deg2Rad) * Vector3.right + Mathf.Cos(phi * Mathf.Deg2Rad) * Vector3.forward)) * range;
                                ray.Add(fillPos);
                                deviationTheta -= desiredDeltaTheta;
                            }
                            while (deviationTheta >= theta + desiredDeltaTheta);
                            deviationTheta = theta - deltaTheta * 0.5f + desiredDeltaTheta * 0.5f;
                            do
                            {
                                Vector3 fillPos = (Mathf.Sin(deviationTheta * Mathf.Deg2Rad) * Vector3.up + Mathf.Cos(deviationTheta * Mathf.Deg2Rad) * (Mathf.Sin(phi * Mathf.Deg2Rad) * Vector3.right + Mathf.Cos(phi * Mathf.Deg2Rad) * Vector3.forward)) * range;
                                ray.Add(fillPos);
                                deviationTheta += desiredDeltaTheta;
                            }
                            while (deviationTheta <= theta - desiredDeltaTheta);
                        }
                        else//南北极附近需手动做phi坐标旋转，按照四向补充点位
                        {
                            int deviationPhi = 0;
                            while (deviationPhi < 360)
                            {
                                float deviationTheta = theta + deltaTheta * 0.5f - desiredDeltaTheta * 0.5f;
                                do
                                {
                                    Vector3 fillPos = (Mathf.Sin(deviationTheta * Mathf.Deg2Rad) * Vector3.up + Mathf.Cos(deviationTheta * Mathf.Deg2Rad) * (Mathf.Sin(deviationPhi * Mathf.Deg2Rad) * Vector3.right + Mathf.Cos(deviationPhi * Mathf.Deg2Rad) * Vector3.forward)) * range;
                                    ray.Add(fillPos);
                                    deviationTheta -= desiredDeltaTheta;
                                }
                                while (deviationTheta >= theta + desiredDeltaTheta);
                                deviationTheta = theta - deltaTheta * 0.5f + desiredDeltaTheta * 0.5f;
                                do
                                {
                                    Vector3 fillPos = (Mathf.Sin(deviationTheta * Mathf.Deg2Rad) * Vector3.up + Mathf.Cos(deviationTheta * Mathf.Deg2Rad) * (Mathf.Sin(deviationPhi * Mathf.Deg2Rad) * Vector3.right + Mathf.Cos(deviationPhi * Mathf.Deg2Rad) * Vector3.forward)) * range;
                                    ray.Add(fillPos);
                                    deviationTheta += desiredDeltaTheta;
                                }
                                while (deviationTheta <= theta - desiredDeltaTheta);
                                deviationPhi += 90;
                            }
                        }
                    }
                    if (range == maxRange)
                        break;
                    range += deltaRange;
                    if (range > maxRange)
                        range = maxRange;
                }
                _rayDotsLocalPositon.Add(ray);
                phi += deltaPhi;
            }
            theta += deltaTheta;
        }

        if (rayDotsLocalPositon != null)
        {
            foreach (Vector3[] ray in rayDotsLocalPositon)
                Array.Clear(ray, 0, ray.Length);
            Array.Clear(rayDotsLocalPositon, 0, rayDotsLocalPositon.Length);
        }
        else
            rayDotsLocalPositon = new Vector3[_rayDotsLocalPositon.Count][];

        for (int indexRay = 0; indexRay < rayDotsLocalPositon.Length; indexRay += 1)
        {
            rayDotsLocalPositon[indexRay] = new Vector3[_rayDotsLocalPositon[indexRay].Count];
            for (int indexDot = 0; indexDot < rayDotsLocalPositon[indexRay].Length; indexDot += 1)
            {
                rayDotsLocalPositon[indexRay][indexDot] = _rayDotsLocalPositon[indexRay][indexDot];
            }
            _rayDotsLocalPositon[indexRay].Clear();
            _rayDotsLocalPositon[indexRay] = null;
        }
        _rayDotsLocalPositon.Clear();
        _rayDotsLocalPositon = null;
    }
    public void PredictGroundBlockCoverage_Sphere()
    {
        blocksCoveredByThis_predicted.Clear();

        int startRay, endRay;
        if (lightTrans.position.y > Map.halfEdgeLengthY_block)
        {
            startRay = 0;
            endRay = Mathf.FloorToInt(rayDotsLocalPositon.Length * 0.5f);
        }
        else if (lightTrans.position.y < -Map.halfEdgeLengthY_block)
        {
            startRay = Mathf.CeilToInt(rayDotsLocalPositon.Length * 0.5f);
            endRay = rayDotsLocalPositon.Length - 1;
        }
        else
        {
            startRay = 0;
            endRay = rayDotsLocalPositon.Length - 1;
        }

        for (int indexRay = startRay; indexRay <= endRay; indexRay += 1)
        {
            for (int indexDot = 0; indexDot < rayDotsLocalPositon[indexRay].Length; indexDot += 1)
            {
                Vector3 dotWorldPos = lightTrans.position + rayDotsLocalPositon[indexRay][indexDot];
                Block blockHovered = Map.Block_FromPos(dotWorldPos);

                //Obstruction检测。若被遮蔽，略过当前ray后面的dot
                if (blockHovered.obstructionsHoveringThis != null)
                {
                    foreach (Obstruction obstruction in blockHovered.obstructionsHoveringThis)
                    {
                        if (obstruction.CheckContainDot(dotWorldPos) == true)
                            goto GoToNextRay;
                    }
                }

                //GroundBlock检测
                if (Mathf.Abs(dotWorldPos.y) <= Map.halfEdgeLengthY_block)
                {
                    if (blocksCoveredByThis_predicted.Contains(blockHovered) == false)
                        blocksCoveredByThis_predicted.Add(blockHovered);
                }
            }
        GoToNextRay:
            continue;
        }
    }

    /*SpotLamp专用方法*/
    public void SetRayDotsLocalPosition_Spot()
    {
        List<List<Vector3>> _rayDotsLocalPositon = new();

        float halfSpotAngle = lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad;

        float maxRange = lightComponent.range;
        float minRange = maxRange > 0.5f ? 0.5f : maxRange * 0.5f;
        const float deltaRange = Map.scanSpacing;

        float deltaTheta;
        if (maxRange * halfSpotAngle > deltaRange)
        {
            deltaTheta = deltaRange / maxRange;
            deltaTheta = halfSpotAngle / Mathf.Ceil(halfSpotAngle / deltaTheta);
        }
        else
            deltaTheta = halfSpotAngle;

        //theta从0到spotAngle/2，phi从0到360，range从minRange到maxRange
        float theta = 0f;
        while (theta <= halfSpotAngle)
        {
            float deltaPhi;
            if (theta == 0f)
                deltaPhi = 10f;// 10f > 2pi，只会进行一次计算
            else
            {
                deltaPhi = 1f / Mathf.Ceil((maxRange * Mathf.Sin(theta)) / deltaRange);
                if (deltaPhi > Mathf.PI * 0.25f && deltaPhi < Mathf.PI * 2f)
                    deltaPhi = Mathf.PI * 0.25f;
            }

            float phi = 0;
            while (phi < Mathf.PI * 2f)
            {
                List<Vector3> ray = new();
                float range = minRange;
                while (range <= maxRange)
                {
                    //只需计算ray线上点位
                    Vector3 pos = (Mathf.Cos(theta) * Vector3.forward + Mathf.Sin(theta) * (-Mathf.Cos(phi) * Vector3.up + Mathf.Sin(phi) * Vector3.right)) * range;
                    ray.Add(pos);

                    range = range + deltaRange > maxRange && range != maxRange ? maxRange : range + deltaRange;
                }
                _rayDotsLocalPositon.Add(ray);

                if (phi < Mathf.PI * 2 - deltaPhi * 1.5f)//允许的最大phi值为 2pi - deltaPhi/2
                    phi += deltaPhi;
                else
                    break;
            }

            if (theta == halfSpotAngle)
                break;
            theta += deltaTheta;
            if (theta > halfSpotAngle)
                theta = halfSpotAngle;
        }

        if (rayDotsLocalPositon != null)
        {
            foreach (Vector3[] ray in rayDotsLocalPositon)
                Array.Clear(ray, 0, ray.Length);
            Array.Clear(rayDotsLocalPositon, 0, rayDotsLocalPositon.Length);
        }
        else
            rayDotsLocalPositon = new Vector3[_rayDotsLocalPositon.Count][];

        for (int indexRay = 0; indexRay < rayDotsLocalPositon.Length; indexRay += 1)
        {
            rayDotsLocalPositon[indexRay] = new Vector3[_rayDotsLocalPositon[indexRay].Count];
            for (int indexDot = 0; indexDot < rayDotsLocalPositon[indexRay].Length; indexDot += 1)
            {
                rayDotsLocalPositon[indexRay][indexDot] = _rayDotsLocalPositon[indexRay][indexDot];
            }
            _rayDotsLocalPositon[indexRay].Clear();
            _rayDotsLocalPositon[indexRay] = null;
        }
        _rayDotsLocalPositon.Clear();
        _rayDotsLocalPositon = null;
    }
    public void PredictGroundBlockCoverage_Spot()
    {
        blocksCoveredByThis_predicted.Clear();

        Vector3 lightTransRight = lightTrans.right;
        Vector3 lightTransUp = lightTrans.up;
        Vector3 lightTransForward = lightTrans.forward;

        foreach (Vector3[] ray in rayDotsLocalPositon)
        {
            foreach (Vector3 dotLoclaPos in ray)
            {
                Vector3 dotWorldPos = lightTrans.position + dotLoclaPos.x * lightTransRight + dotLoclaPos.y * lightTransUp + dotLoclaPos.z * lightTransForward;
                Block blockHovered = Map.Block_FromPos(dotWorldPos);

                //Obstruction检测。若被遮蔽，略过当前ray后面的dot
                if (blockHovered.obstructionsHoveringThis != null)
                {
                    foreach (Obstruction obstruction in blockHovered.obstructionsHoveringThis)
                    {
                        if (obstruction.CheckContainDot(dotWorldPos) == true)
                            goto GoToNextRay;
                    }
                }

                //GroundBlock检测
                if (Mathf.Abs(dotWorldPos.y) <= Map.halfEdgeLengthY_block)
                {
                    if (blocksCoveredByThis_predicted.Contains(blockHovered) == false)
                        blocksCoveredByThis_predicted.Add(blockHovered);
                }
            }
        GoToNextRay:
            continue;
        }
    }
}

public class AndroidWithLamp : Lamp
{
    public CharacterController androidCharacterController;
    public Transform androidTransform;

    public AndroidWithLamp(GameObject lampObject, float solidProbability) : base(lampObject, solidProbability)
    {
        androidCharacterController = lampObject.GetComponent<CharacterController>();
        androidTransform = lampObject.transform;
    }
}