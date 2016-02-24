//------------------------------------------------------------------------------
//  Copyright (c) 2014-2015 the original author or authors. All Rights Reserved. 
// 
//  NOTICE: You are permitted to use, modify, and distribute this file 
//  in accordance with the terms of the license agreement accompanying it. 
//------------------------------------------------------------------------------

namespace Robotlegs.Bender.Framework.API
{
	public enum LifecycleState
	{
		UNINITIALIZED,
		INITIALIZING,
		ACTIVE,
		SUSPENDING,
		SUSPENDED,
		RESUMING,
		DESTROYING,
		DESTROYED
	}
}

