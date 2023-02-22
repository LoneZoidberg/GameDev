using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Obstruction;

/*����ί��*/
public delegate void Delegate_Obstruction_SetGroundBlockHoverage();
public delegate bool Delegate_Obstruction_CheckContainDot(Vector3 dotPos);

public class Obstruction
{
    /*��ı���*/
    //ͨ�ñ���
    readonly GameObject obstructionObject;
    Vector3 centerPos;
    Quaternion rotation;
    int num_bricksHoveredByThis;//����·�ʧȥbrick֧�ţ���׹����ʧ
    public Delegate_Obstruction_SetGroundBlockHoverage SetGroundBlockHoverage;//�޸�Block��� public List<Obstruction> obstructionsHoveringThis;
    public Delegate_Obstruction_CheckContainDot CheckContainDot;

    //ֻ��Box��Ч
    float boxHalfX;
    float boxHalfY;
    float boxHalfZ;
    Vector3 boxRight;
    Vector3 boxUp;
    Vector3 boxForward;

    //ֻ��Capsule��Ч
    float capsuleHalfHeight;
    Vector3 capsuleDirection;

    //Capsule��Sphere����
    float radius;

    /*���캯��*/
    public Obstruction(GameObject obstructionObject)
    {
        this.obstructionObject = obstructionObject;
        centerPos = obstructionObject.transform.position;//��Ӧ������ײ���ģ�����Ĳ���ͬһλ�õ����
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
        else if (obstructionObject.TryGetComponent<CapsuleCollider>(out CapsuleCollider _capsuleCollider))//��ΪԲ����
        {
            capsuleHalfHeight = _capsuleCollider.height * 0.5f * obstructionObject.transform.lossyScale[_capsuleCollider.direction];
            //��Ӧ����Ǿ���������������Ų�ͬ�����
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
            //��Ӧ����ͬ�������Ų�ͬ�����
            radius = _sphereCollider.radius * obstructionObject.transform.lossyScale[0];

            SetGroundBlockHoverage = SetGroundBlockHoverage_Sphere;
            CheckContainDot = CheckContainDot_Sphere;
        }
        else
        {
            UnityEngine.Debug.Log("��ȡ��ײ���������" + obstructionObject.name + "��λ��" + obstructionObject.transform.position);
        }
    }

    /*��ί�е��õĺ���*/
    void SetGroundBlockHoverage_Box()//ֻ�迼��Box�������ڵ����ͶӰ
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
    void SetGroundBlockHoverage_Capsule()//ֻ�迼��Բ���������ڵ����ͶӰ
    {
        float deltaAngle = Map.scanSpacing / radius;
        float angle;
        float h = -capsuleHalfHeight;
        //�����������capsuleDirection������ֱ�ķ���
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
                //����Block����Բ������ĵ�
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
                //����˵��Ƿ���Բ��Χ��
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
    bool CheckContainDot_Capsule(Vector3 dotPos)//��ΪԲ����
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
