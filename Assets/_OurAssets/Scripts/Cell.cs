using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour, ISelectable
{
    public Renderer cellHighlightRend;
    public Color redHighlightColor;
    public Color highlightColor;
    public Color hiddenColor;
    public Transform rangeIndicator_Power;

    public AttackTrigger attackTrigger;

    public int x, y;
    public bool placeableArea = true;
    public bool waterArea = false;

    internal bool isPowered;
    internal bool buildable = true;
    internal bool hasStructure;
    internal bool isChild = false;
    internal GameObject currentStructureObject;
    internal Cell parentCell;

    internal bool isParent = false;
    internal Cell[] childrenCells;

    internal StructurePropertyScriptableObject structureProperty;

    GroundUnit target;
    float currentAttackTime = 0.0f;

    void Update()
    {
        if(target != null && isPowered)
        {
            currentAttackTime -= Time.deltaTime;
            if(currentAttackTime <= 0.0f)
            {
                Attack();
            }
        }
    }

    void Attack()
    {
        currentAttackTime = structureProperty.attackProperties.attackSpeed;
        GameObject projectileObject = Instantiate(structureProperty.attackProperties.projectile, transform);

        Projectile projectile = projectileObject.GetComponent<Projectile>();
        projectile.Fire(target, structureProperty, GetMidPoint(), structureProperty.bonusProperties) ;
    }

    public void DetermineBuildable()
    {
        SetBuildable(CheckStructure() && CheckRestrictions() && CheckPower() && placeableArea && GameManager.HasEnoughMoney(GameManager.currentStructure.cost));
    }

    bool CheckStructure()
    {
        return !hasStructure && !isChild;
    }

    bool CheckRestrictions()
    {

        if (GameManager.currentStructure.buildRestrictions == StructurePropertyScriptableObject.LocationRestrictions.None)
            return true;
        else
        {
            if (GameManager.currentStructure.buildRestrictions == StructurePropertyScriptableObject.LocationRestrictions.Water)
            {
                return waterArea;
            }
            else
            {
                return !waterArea;
            }
        }
    }

    bool CheckPower()
    {
        if (GameManager.currentStructure.powerStructure)
            return true;
        else
            return isPowered;
    }

    public void SetBuildable(bool isBuildable)
    {
        buildable = isBuildable;
        cellHighlightRend.material.color = buildable ? highlightColor : redHighlightColor;
    }

    public void DisableHighlight()
    {
        cellHighlightRend.material.color = hiddenColor;
    }

    public void SetStructure(int structureID)
    {
        hasStructure = true;
        structureProperty = GameManager.singleton.structureProperties[structureID];
        if (structureProperty.powerStructure)
        {
            PowerArea();
        }
        else
        {
            attackTrigger.subTrigger.gameObject.SetActive(true);
            attackTrigger.subTrigger.onTrigger += SubTrigger_onTrigger;
            ShowRange();
            HideRange();
        }

        currentStructureObject = Instantiate(structureProperty.structureObject, transform);
        currentStructureObject.transform.position = GetMidPoint();
        currentStructureObject.transform.localScale *= structureProperty.size;
        LeanTween.scale(currentStructureObject, currentStructureObject.transform.localScale * 1.25f, 1.0f).setEasePunch();
    }

    private void SubTrigger_onTrigger(Collider2D collider, bool entered)
    {

        if (collider.CompareTag("GroundUnit"))
        {
            if (entered)
            {
                GroundUnit newUnit = collider.GetComponent<GroundUnit>();
                if (target == null)
                {
                    if (!newUnit.satisfied || !newUnit.fullySatisfied)
                    {
                        AssignNewTarget(newUnit);
                    }
                }
                else if (target.satisfied & !newUnit.satisfied)
                {
                    RemoveCurrentTarget(false);
                    AssignNewTarget(newUnit);
                }
            }
            else if (target != null && target.myCollider == collider)
            {
                RemoveCurrentTarget();
            }
        }
    }

    void AssignNewTarget(GroundUnit newUnit)
    {
        target = newUnit;
        target.onSatisfied += TargetSatisfied;
        target.onFullySatisfied += TargetFullySatisfied;
        target.onFinishedPath += TargetFinishedPath;
    }

    void TargetFinishedPath(GroundUnit thisUnit)
    {
        if (thisUnit == target)
        {
            RemoveCurrentTarget();
        }
    }

    private void TargetSatisfied(GroundUnit thisUnit)
    {
        if(thisUnit == target)
        {
            RemoveCurrentTarget();
        }
    }

    private void TargetFullySatisfied(GroundUnit thisUnit)
    {
        if(thisUnit == target)
        {
            RemoveCurrentTarget();
        }
    }

    void RemoveCurrentTarget(bool resetCollider = true)
    {
        target.onFullySatisfied -= TargetFullySatisfied;
        target.onFinishedPath -= TargetFinishedPath;
        target = null;

        if (resetCollider)
        {
            attackTrigger.attackCollider.enabled = false;
            attackTrigger.attackCollider.enabled = true;
        }
    }

    internal void ShowRange()
    {
        if (structureProperty.powerStructure)
        {
            rangeIndicator_Power.gameObject.SetActive(true);
            rangeIndicator_Power.transform.parent = null;
            rangeIndicator_Power.transform.position = GetMidPoint();

            rangeIndicator_Power.transform.localScale = Vector3.one * ((structureProperty.range * 2) + 1 + ((structureProperty.size - 1) * .7f));
            rangeIndicator_Power.transform.parent = transform;
        }
        else
        {
            attackTrigger.triggerRend.enabled = true;
            attackTrigger.rangeIndicator_Attack.transform.parent = null;
            attackTrigger.rangeIndicator_Attack.transform.position = GetMidPoint();

            attackTrigger.rangeIndicator_Attack.transform.localScale = Vector3.one * structureProperty.range * 3f;
            attackTrigger.rangeIndicator_Attack.transform.parent = transform;
        }
    }

    internal void HideRange()
    {
        rangeIndicator_Power.gameObject.SetActive(false);
        attackTrigger.triggerRend.enabled = false;
    }

    public void Select()
    {
        //Item Selected
        if (isChild)
        {
            parentCell.Select();
        }
        else
        {
            LeanTween.scale(currentStructureObject, currentStructureObject.transform.localScale * 1.25f, 1.0f).setEasePunch();
            GameManager.singleton.SelectStructure(this);
            ShowRange();
        }
    }

    public void Deselect()
    {
        //Item Selected
        GameManager.singleton.DeselectStructure();
        HideRange();
    }

    internal void SellStructure()
    {
        if(structureProperty != null)
        {
            hasStructure = false;
            Destroy(currentStructureObject);

            if (isParent)
            {
                for (int i = 0; i < childrenCells.Length; i++)
                {
                    childrenCells[i].hasStructure = false;
                    childrenCells[i].isChild = false;
                }
            }

            if (structureProperty.powerStructure)
            {
                List<Collider2D> nearbyCells = new List<Collider2D>();
                ContactFilter2D contactFilter = new ContactFilter2D();
                if (Physics2D.OverlapBox(GetMidPoint(), Vector2.one * ((structureProperty.range * 2) + ((structureProperty.size - 1) * .5f) - 1), 0, contactFilter, nearbyCells) > 0)
                {
                    for (int i = 0; i < nearbyCells.Count; i++)
                    {
                        if (nearbyCells[i].CompareTag("Selectable"))
                        {
                            Cell cell = nearbyCells[i].GetComponent<Cell>();
                            if (cell != null)
                            {
                                cell.isPowered = false;
                            }
                        }
                    }
                }
            }
            else
            {
                target = null;
                attackTrigger.subTrigger.gameObject.SetActive(false);
                attackTrigger.subTrigger.onTrigger -= SubTrigger_onTrigger;
            }

            GameManager.singleton.AddPollution(-structureProperty.pollution);
            GameManager.singleton.AddMoney(structureProperty.sellPrice);
            structureProperty = null;
            GridInteraction.singleton.SellSelected();
        }
    }

    internal void PowerArea()
    {
        if (structureProperty.powerStructure)
        {
            List<Collider2D> nearbyCells = new List<Collider2D>();
            ContactFilter2D contactFilter = new ContactFilter2D();
            if (Physics2D.OverlapBox(GetMidPoint(), Vector2.one * ((structureProperty.range * 2) + ((structureProperty.size - 1) * .5f) - 1), 0, contactFilter, nearbyCells) > 0)
            {
                for (int i = 0; i < nearbyCells.Count; i++)
                {
                    if (nearbyCells[i].CompareTag("Selectable"))
                    {
                        Cell cell = nearbyCells[i].GetComponent<Cell>();
                        if (cell != null)
                        {
                            cell.isPowered = true;
                        }
                    }
                }
            }
        }
    }

    internal Vector3 GetMidPoint()
    {
        if (isParent)
        {
            Vector3 returnVector = Vector3.zero;
            for (int i = 0; i < childrenCells.Length; i++)
            {
                returnVector += childrenCells[i].transform.position;
            }
            return returnVector / childrenCells.Length;
        }
        else
        {
            return transform.position;
        }
    }
}

[System.Serializable]
public class AttackTrigger
{
    public Transform rangeIndicator_Attack;
    public SubTrigger subTrigger;
    public SpriteRenderer triggerRend;
    public Collider2D attackCollider;
}