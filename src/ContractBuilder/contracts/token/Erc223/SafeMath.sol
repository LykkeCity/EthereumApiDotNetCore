pragma solidity ^0.4.11;


/**
 * Math operations with safety checks
 */
library SafeMath
{
    uint256 constant public MAX_UINT256 =
    0xFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF;

    function GET_MAX_UINT256() pure internal returns(uint256){
        return MAX_UINT256;
    }

    function mul(uint a, uint b) internal returns(uint){
        uint c = a * b;
        assertSafe(a == 0 || c / a == b);
        return c;
    }

    function div(uint a, uint b) pure internal returns(uint){
        // assert(b > 0); // Solidity automatically throws when dividing by 0
        uint c = a / b;
        // assert(a == b * c + a % b); // There is no case in which this doesn't hold
        return c;
    }

    function sub(uint a, uint b) internal returns(uint){
        assertSafe(b <= a);
        return a - b;
    }

    function add(uint a, uint b) internal returns(uint){
        uint c = a + b;
        assertSafe(c >= a);
        return c;
    }

    function max64(uint64 a, uint64 b) internal view returns(uint64){
        return a >= b ? a : b;
    }

    function min64(uint64 a, uint64 b) internal view returns(uint64){
        return a < b ? a : b;
    }

    function max256(uint256 a, uint256 b) internal view returns(uint256){
        return a >= b ? a : b;
    }

    function min256(uint256 a, uint256 b) internal view returns(uint256){
        return a < b ? a : b;
    }

    function assertSafe(bool assertion) internal {
        if (!assertion) {
            revert();
        }
    }
}
