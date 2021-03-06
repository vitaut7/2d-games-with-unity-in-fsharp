﻿namespace HalpernRPG.MonoBehaviours

open UnityEngine
open System.Collections
open HalpernRPG

[<RequireComponent(typeof<Rigidbody2D>)>]
[<RequireComponent(typeof<CircleCollider2D>)>]
[<RequireComponent(typeof<Animator>)>]
type Wander() as this = 
    inherit MonoBehaviour()

    [<DefaultValue>]val mutable pursuitSpeed : float32
    [<DefaultValue>]val mutable wanderSpeed : float32
    let mutable currentSpeed = 0.f

    [<DefaultValue>]val mutable directionChangeInterval : float32
    [<DefaultValue>]val mutable followPlayer : bool
    let mutable moveCoroutine = Unchecked.defaultof<Coroutine>

    let mutable rb2d = Unchecked.defaultof<Rigidbody2D>
    let mutable animator = Unchecked.defaultof<Animator>
    let mutable circleCollider = Unchecked.defaultof<CircleCollider2D>

    let mutable targetTransform = Unchecked.defaultof<Transform>
    let mutable endPosition = Unchecked.defaultof<Vector2>
    let mutable currentAngle = 0.f

    let vector2FromAngle angleDegrees =
        let inputAngleRadians = angleDegrees * Mathf.Deg2Rad
        Vector2 (Mathf.Cos inputAngleRadians, Mathf.Sin inputAngleRadians)

    let chooseNewEndpoint () =
        currentAngle <- currentAngle + float32 (Random.Range (0, 360))
        currentAngle <- Mathf.Repeat (currentAngle, 360.f)
        endPosition <- vector2FromAngle currentAngle

    let move () =
        let remainingDistance () = 
            (Vector.from3to2 this.transform.position - endPosition).sqrMagnitude
        seq {
            while remainingDistance () > 0.f do
                if not (isNull targetTransform) then
                    endPosition <- Vector.from3to2 targetTransform.position
                if not (isNull rb2d) then
                    animator.SetBool ("isWalking", true)
                    let newPosition = Vector2.MoveTowards (rb2d.position, endPosition, currentSpeed * Time.deltaTime)
                    rb2d.MovePosition newPosition
                yield WaitForFixedUpdate ()
            animator.SetBool ("isWalking", false)
        } :?> IEnumerator

    let wanderRoutine () =
        seq {
            while true do
                chooseNewEndpoint ()
                if not (isNull moveCoroutine) then
                    this.StopCoroutine moveCoroutine
                moveCoroutine <- this.StartCoroutine (move ())
                yield WaitForSeconds this.directionChangeInterval
        } :?> IEnumerator

    member this.Start () =
        animator <- this.GetComponent<Animator> ()
        rb2d <- this.GetComponent<Rigidbody2D> ()
        circleCollider <- this.GetComponent<CircleCollider2D> ()

        currentSpeed <- this.wanderSpeed
        this.StartCoroutine (wanderRoutine ())

    member this.OnTriggerEnter2D (collision: Collider2D) =
        if this.followPlayer && collision.gameObject.CompareTag "Player" then
            currentSpeed <- this.pursuitSpeed
            targetTransform <- collision.gameObject.transform

            if not (isNull moveCoroutine) then
                this.StopCoroutine moveCoroutine
            moveCoroutine <- this.StartCoroutine (move ())

    member this.OnTriggerExit2D (collision: Collider2D) =
        if this.followPlayer && collision.gameObject.CompareTag "Player" then
            animator.SetBool ("isWalking", false)
            currentSpeed <- this.wanderSpeed

            if not (isNull moveCoroutine) then
                this.StopCoroutine moveCoroutine
            moveCoroutine <- this.StartCoroutine (move ())
            targetTransform <- null

    member this.OnDrawGizmos () =
        if not (isNull circleCollider) then
            Gizmos.DrawWireSphere (this.transform.position, circleCollider.radius)
        Gizmos.DrawLine (Vector.from2to3 rb2d.position, Vector.from2to3 endPosition)