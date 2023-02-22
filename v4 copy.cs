using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Obstruction;

/*声明委托*/
public delegate void Delegate_Obstruction_SetGroundBlockHoverage();
public delegate bool Delegate_Obstruction_CheckContainDot(Vector3 dotPos);

public class Obstruction
{
    /*类的变量*/
    //通用变量
    readonly GameObject obstructionObject;
    Vector3 centerPos;
    Quaternion rotation;
    int num_bricksHoveredByThis;//如果下方失去brick支撑，则坠落消失
    public Delegate_Obstruction_SetGroundBlockHoverage SetGroundBlockHoverage;//修改Block类的 public List<Obstruction> obstructionsHoveringThis;
    public Delegate_Obstruction_CheckContainDot CheckContainDot;

    //只对Box有效
    float boxHalfX;
    float boxHalfY;
    float boxHalfZ;
    Vector3 boxRight;
    Vector3 boxUp;
    Vector3 boxForward;

    //只对Capsule有效
    float capsuleHalfHeight;
    Vector3 capsuleDirection;

    //Capsule和Sphere共用
    float radius;

    /*构造函数*/
    public Obstruction(GameObject obstructionObject)
    {
        this.obstructionObject = obstructionObject;
        centerPos = obstructionObject.transform.position;//不应允许碰撞体和模型中心不在同一位置的情况
        rotation = obstructionObject.transform.rotation;

        if (obstructionObject.TryGetComponent<BoxCollider>(out BoxCollider _boxCollider))
        {
            boxHalfX = _boxCollider.size.x * 0.5f * _boxCollider.transform.lossyScale.x;
            boxHalfY = _boxCollider.size.y * 0.5f * _boxCollider.transform.lossyScale.y;
            boxHalfZ = _boxCollider.size.z * 0.5f * _boxCollider.transform.lossyScale.z;
            boxRight = obstructionObject.transform.right;
            boxUp = obstructionObject.transform.up;
            boxForward = obstructionObject.transform.forward;

            SetGroundBlockHoverage = SetGroundBlockHoverage_Box;
            CheckContainDot = CheckContainDot_Box;
        }
        else if (obstructionObject.TryGetComponent<CapsuleCollider>(out CapsuleCollider _capsuleCollider))//简化为圆柱体
        {
            capsuleHalfHeight = _capsuleCollider.height * 0.5f * obstructionObject.transform.lossyScale[_capsuleCollider.direction];
            //不应允许非径向的两个方向缩放不同的情况
            radius = _capsuleCollider.direction == 1 ? _capsuleCollider.radius * obstructionObject.transform.lossyScale[0] : _capsuleCollider.radius * obstructionObject.transform.lossyScale[1];
            if (_capsuleCollider.direction == 0)
                capsuleDirection = obstructionObject.transform.right;
            else if (_capsuleCollider.direction == 1)
                capsuleDirection = obstructionObject.transform.up;
            else
                capsuleDirection = obstructionObject.transform.forward;

            SetGroundBlockHoverage = SetGroundBlockHoverage_Capsule;
            CheckContainDot = CheckContainDot_Capsule;
        }
        else if (obstructionObject.TryGetComponent<SphereCollider>(out SphereCollider _sphereCollider))
        {
            //不应允许不同方向缩放不同的情况
            radius = _sphereCollider.radius * obstructionObject.transform.lossyScale[0];

            SetGroundBlockHoverage = SetGroundBlockHoverage_Sphere;
            CheckContainDot = CheckContainDot_Sphere;
        }
        else
        {
            UnityEngine.Debug.Log("获取碰撞体组件错误：" + obstructionObject.name + "，位于" + obstructionObject.transform.position);
        }
    }

    /*被委托调用的函数*/
    void SetGroundBlockHoverage_Box()//只需考虑Box六个面在地面的投影
    {
        float x = -boxHalfX;
        float y;
        float z;

        while (x <= boxHalfX)
        {
            y = -boxHalfY;
            while (y <= boxHalfY)
            {
                z = -boxHalfZ;
                while (z <= boxHalfZ)
                {
                    Vector3 dotPos = centerPos + x * boxRight + y * boxUp + z * boxForward;
                    int c = Map.Column_FromPosX(dotPos.x);
                    int r = Map.Row_FromPosZ(dotPos.z);
                    if (Map.blocks[c][r].obstructionsHoveringThis == null)
                        Map.blocks[c][r].obstructionsHoveringThis = new() { this };
                    else if (Map.blocks[c][r].obstructionsHoveringThis.Contains(this) == false)
                        Map.blocks[c][r].obstructionsHoveringThis.Add(this);

                    if (z == boxHalfZ)
                        break;
                    if (Mathf.Abs(x) == boxHalfX || Mathf.Abs(y) == boxHalfY)
                        z += Map.scanSpacing;
                    else
                        z = boxHalfZ;
                    if (z > boxHalfZ)
                        z = boxHalfZ;
                }
                if (y == boxHalfY)
                    break;
                y += Map.scanSpacing;
                if (y > boxHalfY)
                    y = boxHalfY;
            }
            if (x == boxHalfX)
                break;
            x += Map.scanSpacing;
            if (x > boxHalfX)
                x = boxHalfX;
        }
    }
    void SetGroundBlockHoverage_Capsule()//只需考虑圆柱体柱面在地面的投影
    {
        float deltaAngle = Map.scanSpacing / radius;
        float angle;
        float h = -capsuleHalfHeight;
        //给出任意的与capsuleDirection两两垂直的方向
        Vector3 v0 = Vector3.Cross(capsuleDirection, new(capsuleDirection.z, capsuleDirection.x, capsuleDirection.y)).normalized;
        Vector3 v1 = Vector3.Cross(capsuleDirection, v0).normalized;

        while (h <= capsuleHalfHeight)
        {
            angle = 0f;
            while (angle < Mathf.PI * 2)
            {
                Vector3 dotPos = centerPos + h * capsuleDirection + (v0 * Mathf.Sin(angle) + v1 * Mathf.Cos(angle)) * radius;
                int c = Map.Column_FromPosX(dotPos.x);
                int r = Map.Row_FromPosZ(dotPos.z);
                if (Map.blocks[c][r].obstructionsHoveringThis == null)
                    Map.blocks[c][r].obstructionsHoveringThis = new() { this };
                else if (Map.blocks[c][r].obstructionsHoveringThis.Contains(this) == false)
                    Map.blocks[c][r].obstructionsHoveringThis.Add(this);

                angle += deltaAngle;
            }

            if (h == capsuleHalfHeight)
                break;
            h += Map.scanSpacing;
            if (h > capsuleHalfHeight)
                h = capsuleHalfHeight;
        }
    }
    void SetGroundBlockHoverage_Sphere()
    {
        int leftLimit = Map.Column_FromPosX(centerPos.x - radius);
        int rightLimit = Map.Column_FromPosX(centerPos.x + radius);
        int downLimit = Map.Row_FromPosZ(centerPos.z - radius);
        int upLimit = Map.Row_FromPosZ(centerPos.z + radius);
        for (int c = leftLimit; c <= rightLimit; c++)
        {
            for (int r = downLimit; r <= upLimit; r++)
            {
                float blockPosX = c * Map.edgeLengthX_block + Map.halfEdgeLengthX_block;
                float blockPosZ = r * Map.edgeLengthZ_block + Map.halfEdgeLengthZ_block;
                //计算Block中与圆心最近的点
                float offsetX;
                if (centerPos.x > blockPosX + Map.halfEdgeLengthX_block)
                    offsetX = Map.halfEdgeLengthX_block;
                else if (centerPos.x < blockPosX - Map.halfEdgeLengthX_block)
                    offsetX = -Map.halfEdgeLengthX_block;
                else
                    offsetX = 0f;
                float offsetZ;
                if (centerPos.z > blockPosZ + Map.halfEdgeLengthZ_block)
                    offsetZ = Map.halfEdgeLengthZ_block;
                else if (centerPos.z < blockPosZ - Map.halfEdgeLengthZ_block)
                    offsetZ = -Map.halfEdgeLengthZ_block;
                else
                    offsetZ = 0f;
                //计算此点是否在圆球范围内
                if ((blockPosX + offsetX - centerPos.x) * (blockPosX + offsetX - centerPos.x) + (blockPosZ + offsetZ - centerPos.z) * (blockPosZ + offsetZ - centerPos.z) <= radius * radius)
                {
                    if (Map.blocks[c][r].obstructionsHoveringThis == null)
                        Map.blocks[c][r].obstructionsHoveringThis = new() { this };
                    else if (Map.blocks[c][r].obstructionsHoveringThis.Contains(this) == false)
                        Map.blocks[c][r].obstructionsHoveringThis.Add(this);
                }
            }
        }
    }
    bool CheckContainDot_Box(Vector3 dotPos)
    {
        dotPos = Quaternion.Inverse(rotation) * (dotPos - centerPos);
        return Mathf.Abs(dotPos.x) <= boxHalfX && Mathf.Abs(dotPos.y) <= boxHalfY && Mathf.Abs(dotPos.z) <= boxHalfZ;
    }
    bool CheckContainDot_Capsule(Vector3 dotPos)//简化为圆柱体
    {
        dotPos = Quaternion.Inverse(rotation) * (dotPos - centerPos);
        if (Mathf.Abs(dotPos.y) <= capsuleHalfHeight)
            return dotPos.x * dotPos.x + dotPos.z * dotPos.z <= radius * radius;
        else
            return false;
    }
    bool CheckContainDot_Sphere(Vector3 dotPos)
    {
        return (dotPos - centerPos).sqrMagnitude <= radius * radius;
    }
}
