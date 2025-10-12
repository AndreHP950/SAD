using System.Collections;
using UnityEngine;

public class PlayerBackpack : MonoBehaviour
{
    public Transform packageSlot;
    private GameObject currentPackage;
    public float packageSpeed = 30f;
    public bool isMovingToSlot = false;
    public bool isMovingToMailbox = false;
    private Transform target;
    public float closeDistance = 0.1f;
    GameObject discardTarget;

    private void Start()
    {
        packageSlot = transform.Find("PackageSlot");
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
                target = discardTarget.transform;
            }
            else target = targetObject;

            currentPackage.transform.SetParent(null);

            isMovingToMailbox = true;
        }
    }

    private void MovePackageToSlot(GameObject package)
    {
        target = packageSlot;

        if (Vector3.Distance(package.transform.position, target.position) > closeDistance)
        {
            Vector3 direction = (target.position - package.transform.position).normalized;
            package.transform.Translate(direction * packageSpeed * Time.deltaTime, Space.World);

            package.transform.rotation = Quaternion.Slerp(package.transform.rotation, target.rotation, Time.deltaTime * 5f);
        }
        else
        {
            package.transform.SetParent(target);
            package.transform.localPosition = Vector3.zero;
            package.transform.localRotation = Quaternion.identity;
            isMovingToSlot = false;
        }
            
    }

    private void MovePackageToMailbox(GameObject package, Transform target)
    {
        if (Vector3.Distance(package.transform.position, target.position) > closeDistance)
        {
            Vector3 direction = (target.position - package.transform.position).normalized;
            package.transform.Translate(direction * packageSpeed * Time.deltaTime, Space.World);

            package.transform.rotation = Quaternion.Slerp(package.transform.rotation, target.rotation, Time.deltaTime * 5f);
        }
        else
        {
            isMovingToMailbox = false;
            Destroy(package);
            currentPackage = null;
        }
    }
}
