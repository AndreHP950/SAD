using System.Collections;
using UnityEngine;

public class PlayerBackpack : MonoBehaviour
{
    public Animator animator;
    public Transform packageSlotLeft;
    public Transform packageSlotRight;
    private Transform packageSlot;
    private GameObject currentPackage;
    public float packageSpeed = 30f;
    public bool isMovingToSlot = false;
    public bool isMovingToMailbox = false;
    private Transform target;
    public float closeDistance = 0.1f;
    GameObject discardTarget;

    private void Start()
    {
        animator = GetComponent<Animator>();
        packageSlotLeft = FindDeepChild(transform, "PackageSlot.L");
        packageSlotRight = FindDeepChild(transform, "PackageSlot.R");
        discardTarget = new GameObject("Discard");
    }

    public void Update()
    {
        if (isMovingToSlot)
        {
            MovePackageToSlot(currentPackage);
        }
        if (isMovingToMailbox)
        {
            MovePackageToMailbox(currentPackage, target);
        }
    }

    public void ReceivePackage(GameObject package)
    {
        currentPackage = package;
        isMovingToSlot = true;
    }

    public void DeliverPackage(Transform targetObject)
    {
        if (currentPackage != null)
        {
            if (!targetObject)
            {
                discardTarget.transform.parent = null;
                discardTarget.transform.position = transform.position + new Vector3(0, 15, 0);
                discardTarget.transform.rotation = transform.rotation;
                target = discardTarget.transform;
            }
            else target = targetObject;

            currentPackage.transform.SetParent(null);

            isMovingToMailbox = true;
        }
    }

    private void MovePackageToSlot(GameObject package)
    {
        if (Vector3.Distance(package.transform.position, packageSlotLeft.transform.position) <= Vector3.Distance(package.transform.position, packageSlotRight.transform.position))
            packageSlot = packageSlotLeft;
        else packageSlot = packageSlotRight;

            float step = packageSpeed * Time.deltaTime;
        float distance = Vector3.Distance(package.transform.position, packageSlot.position);

        if (distance > closeDistance)
        {
            package.transform.position = Vector3.MoveTowards(package.transform.position, packageSlot.position, step);

            package.transform.rotation = Quaternion.Slerp(package.transform.rotation, packageSlot.rotation, Time.deltaTime * 5f);
        }
        else
        {
            Debug.Log("Encostou no slot");
            package.transform.SetParent(packageSlot);
            package.transform.localPosition = Vector3.zero;
            package.transform.localRotation = Quaternion.identity;
            isMovingToSlot = false;
        }    
    }

    private void MovePackageToMailbox(GameObject package, Transform target)
    {
        float step = packageSpeed * Time.deltaTime;
        float distance = Vector3.Distance(package.transform.position, target.position);

        if (distance > closeDistance)
        {
            package.transform.position = Vector3.MoveTowards(package.transform.position, target.position, step);

            package.transform.rotation = Quaternion.Slerp(package.transform.rotation, target.rotation, Time.deltaTime * 5f);
        }
        else
        {
            isMovingToMailbox = false;
            Destroy(package);
            currentPackage = null;
        }
    }

    Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
}
