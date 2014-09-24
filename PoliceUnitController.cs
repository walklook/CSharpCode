using UnityEngine;
using System.Collections;

public class PoliceUnitController : UnitController
{
	private Vector3 mOriginalPoint = Vector3.zero;
	
	// Use this for initialization
	void Start ()
	{
		base.Start();
		SetState( UnitController.UnitState.PATROLING );
		mOriginalPoint = transform.position;
	}
	
	// Update is called once per frame
	void Update ()
	{
		if ( this.gameObject == null )
		{
			return;
		}
		
		base.Update();
		
		if ( mState == UnitController.UnitState.PATROLING )
		{
			Vector3 dir = mOriginalPoint - transform.position;
			float angle = Vector3.Angle( dir, mDirection );
			// 5.0f is big enough.
			if ( angle > 5.0f && Vector3.Distance( mOriginalPoint, transform.position ) >= mPatrolDistance )
			{
				Turn();
			}
			transform.position += mSpeed * mDirection * Time.deltaTime;
		}
		else if ( mState == UnitController.UnitState.IDLE )
		{
			
		}
		else if ( mState == UnitController.UnitState.TRACING )
		{
			DebugUtil.Assert( mPath != null, "The path of crew unit is null when tracing!" );

			if ( mTarget.State == UnitController.UnitState.DEAD )
			{
				mTarget = null;
				Transform tran = mCombatSceneController.GetNearestPoliceCar( transform.position );
				mSeeker.StartPath( transform.position, tran.position, OnPathComplete );
				SetState( UnitController.UnitState.WAITFORBACK );
			}
			else
			{
				float dis = Distance( mTarget.transform.position, mPath.vectorPath[mPath.vectorPath.Count - 1] );
				if ( mCurrentWayPoint >= mPath.vectorPath.Count || dis >= 0 )
				{
					Debug.Log( "End of Path reached! This should be recalculated!" );
					mSeeker.StartPath( transform.position, mTarget.transform.position, OnPathComplete );
					SetState( UnitState.IDLE );
				}
				else
				{
					Vector3 dir = ( mPath.vectorPath[mCurrentWayPoint] - transform.position ).normalized;
					dir.y = 0;
					dir *= mAlertSpeed * Time.fixedDeltaTime;
					transform.position += dir;
					
					if ( Distance( transform.position, mPath.vectorPath[mCurrentWayPoint] ) < mNextWayPointDistance )
					{
						mCurrentWayPoint++;
					}
				}
			}
		}
		else if ( mState == UnitController.UnitState.FIGHTING )
		{	
			if ( mFighter.State == UnitController.UnitState.DEAD )
			{
				mFighter = null;
				Transform trans = mCombatSceneController.GetNearestPoliceCar( transform.position );
				//mSeeker.StartPath( transform.position, mOriginalPoint, OnPathComplete );
				mSeeker.StartPath( transform.position, trans.position, OnPathComplete );
				SetState( UnitController.UnitState.WAITFORBACK );
			}
			else
			{
				mFightTimer += Time.deltaTime;
				Vector3 dir = mFighter.transform.position - transform.position;
				mUnitRender.SetDirection( new Vector3( dir.x, 0, dir.z ) );
				if ( mFightTimer > mAttackSpeed )
				{
					Fire();
					mFightTimer = 0.0f;
				}
			}
		}
		else if ( mState == UnitController.UnitState.WAITFORBACK )
		{
			
		}
		else if ( mState == UnitController.UnitState.GOBACK )
		{
			DebugUtil.Assert( mPath != null, "The path of crew unit is null when tracing!" );
			
			if ( mCurrentWayPoint >= mPath.vectorPath.Count )
			{
				Vector3 dir = ( mOriginalPoint - transform.position ).normalized;
				dir.y = 0;
				dir *= mSpeed * Time.fixedDeltaTime;
				transform.position += dir;
			}
			else
			{
				Vector3 dir = ( mPath.vectorPath[mCurrentWayPoint] - transform.position ).normalized;
				dir.y = 0;
				dir *= mSpeed * Time.fixedDeltaTime;
				transform.position += dir;
				
				if ( Vector3.Distance( transform.position, mPath.vectorPath[mCurrentWayPoint] ) < mNextWayPointDistance )
				{
					mCurrentWayPoint++;
				}
			}
			
			if ( Vector3.Distance( mOriginalPoint, transform.position ) <= 0.05f )
			{
				//SetState( UnitController.UnitState.PATROLING );
				SetState( UnitController.UnitState.DISAPPEAR );
			}
			
		}
		else if ( mState == UnitController.UnitState.WAITFORWATCHING )
		{
			
		}
		else if ( mState == UnitController.UnitState.WATCHING )
		{
			float dis = Vector3.Distance( transform.position, mTarget.transform.position );
			if ( dis > mAttackRange )
			{
				mSeeker.StartPath( transform.position, mTarget.transform.position, OnPathComplete );
				SetState( UnitState.IDLE );
			}
		}
		else if ( mState == UnitController.UnitState.DISAPPEAR )
		{
			mCombatSceneController.PoliceUnits.Remove( this );
			DestroyImmediate( this.gameObject );
			return;
		}
		else if ( mState == UnitController.UnitState.DEAD )
		{
			
		}
		
		if ( mState != UnitController.UnitState.FIGHTING && mState != UnitController.UnitState.DEAD )
		{	
			FindTargetWithoutWatchState();
		}
	}
	
	private void FindTargetWithWatchState()
	{
		UnitController uc = mCombatSceneController.GetNearestHostileTarget( transform.position );
		if ( uc != null )
		{	
			float dis = Vector3.Distance( uc.transform.position, transform.position );
			if ( dis <= mAttackRange )
			{
				mTarget = uc;
				if ( uc.State == UnitController.UnitState.FIGHTING )
				{
					Fight( mTarget, UnitController.UnitState.FIGHTING );
				}
				else
				{
					SetState( UnitController.UnitState.WATCHING );
				}
			}
			else if ( mState != UnitState.WATCHING && mState != UnitState.IDLE && mState != UnitState.WAITFORBACK && mState != UnitState.TRACING && dis <= mSpotRadius )
			{
				mTarget = uc;
				mSeeker.StartPath( transform.position, mTarget.transform.position, OnPathComplete );
				SetState( UnitController.UnitState.IDLE );
			}
		}
	}
	
	private void FindTargetWithoutWatchState()
	{
		UnitController uc = mCombatSceneController.GetNearestHostileTarget( transform.position );
		if ( uc != null )
		{	
			float dis = Vector3.Distance( uc.transform.position, transform.position );
			if ( dis <= mAttackRange )
			{
				mTarget = uc;
				Fight( mTarget, UnitController.UnitState.FIGHTING );
			}
			else if ( mState != UnitState.IDLE && mState != UnitState.WAITFORBACK && mState != UnitState.TRACING && dis <= mSpotRadius )
			{
				mTarget = uc;
				mSeeker.StartPath( transform.position, mTarget.transform.position, OnPathComplete );
				SetState( UnitController.UnitState.IDLE );
			}
		}
	}
	
	private void FindTargetInFighting()
	{
		bool isHandled = false;
		foreach ( CrewUnitController cuc in mCombatSceneController.CrewUnits )
		{
			if ( cuc.State == UnitController.UnitState.FIGHTING )
			{
				float dis = Vector3.Distance( transform.position, cuc.transform.position );
				if ( dis < mAttackRange )
				{
					mTarget = cuc;
					Fight( mTarget, UnitController.UnitState.FIGHTING );
					isHandled = true;
					break;
				}
			}
		}
		
		if ( !isHandled )
		{
			foreach ( EnemyUnitController euc in mCombatSceneController.EnemyUnits )
			{
				if ( euc.State == UnitController.UnitState.FIGHTING )
				{
					float dis = Vector3.Distance( transform.position, euc.transform.position );
					if ( dis < mAttackRange )
					{
						mTarget = euc;
						Fight( mTarget, UnitController.UnitState.FIGHTING );
						isHandled = true;
						break;
					}
				}
			}
		}
		
		if ( mState != UnitController.UnitState.TRACING && !WaitingForPathFinding() )
		{
			if ( !isHandled )
			{
				foreach ( CrewUnitController cuc in mCombatSceneController.CrewUnits )
				{
					if ( cuc.State == UnitController.UnitState.FIGHTING )
					{
						float dis = Vector3.Distance( transform.position, cuc.transform.position );
						if ( dis < mSpotRadius )
						{
							mTarget = cuc;
							mSeeker.StartPath( transform.position, mTarget.transform.position, OnPathComplete );
							SetState( UnitState.IDLE );
							isHandled = true;
							break;
						}
					}
				}
			}
			
			if ( !isHandled )
			{
				foreach ( EnemyUnitController euc in mCombatSceneController.EnemyUnits )
				{
					if ( euc.State == UnitController.UnitState.FIGHTING )
					{
						float dis = Vector3.Distance( transform.position, euc.transform.position );
						if ( dis < mSpotRadius )
						{
							mTarget = euc;
							mSeeker.StartPath( transform.position, mTarget.transform.position, OnPathComplete );
							SetState( UnitState.IDLE );
							isHandled = true;
							break;
						}
					}
				}
			}
		}
	}
	
	public override void Fight( UnitController fighter, UnitController.UnitState unitState )
	{
		DebugUtil.Assert( unitState == UnitController.UnitState.FIGHTING, "Unaccepted argument for Fight function in enemy unit!" );
		if ( mState != UnitController.UnitState.FIGHTING )
		{
			base.Fight( fighter, unitState );
		}
	}
