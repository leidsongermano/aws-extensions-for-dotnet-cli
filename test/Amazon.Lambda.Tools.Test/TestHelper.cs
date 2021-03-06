﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Common.DotNetCli.Tools;
using Amazon.Lambda.Tools.Commands;

namespace Amazon.Lambda.Tools.Test
{
    public static class TestHelper
    {
        const string LAMBDATOOL_TEST_ROLE = "dotnet-lambdatools-test-role";

        private static string _roleArn;

        private static IAmazonIdentityManagementService _iamClient = new AmazonIdentityManagementServiceClient(Amazon.RegionEndpoint.USEast1);

        private static readonly object ROLE_LOCK = new object();

        public static string GetTestRoleArn()
        {
            lock (ROLE_LOCK)
            {
                if (!string.IsNullOrEmpty(_roleArn))
                    return _roleArn;

                try
                {
                    _roleArn = (_iamClient.GetRoleAsync(new GetRoleRequest { RoleName = LAMBDATOOL_TEST_ROLE })).Result.Role.Arn;
                }
                catch (Exception e)
                {
                    if (e is NoSuchEntityException || e.InnerException is NoSuchEntityException)
                    {
                        // Role is not found so create a role with no permissions other then Lambda can assume the role. 
                        // The role is deleted and reused in other runs of the test to make the test run faster.
                        _roleArn = RoleHelper.CreateRole(_iamClient, LAMBDATOOL_TEST_ROLE, Constants.LAMBDA_ASSUME_ROLE_POLICY, "arn:aws:iam::aws:policy/PowerUserAccess");

                        // Wait for new role to propogate
                        System.Threading.Thread.Sleep(5000);
                    }
                    else
                    {
                        throw;
                    }
                }

                return _roleArn;
            }
        }

        public static async Task<string> GetPhysicalCloudFormationResourceId(IAmazonCloudFormation cfClient, string stackName, string logicalId)
        {
            var response = await cfClient.DescribeStackResourcesAsync(new DescribeStackResourcesRequest()
            {
                StackName = stackName,
                LogicalResourceId = logicalId
            });

            foreach (var resource in response.StackResources)
            {
                if (string.Equals(resource.LogicalResourceId, logicalId, StringComparison.OrdinalIgnoreCase))
                {
                    return resource.PhysicalResourceId;
                }
            }

            return null;
        }

        public static async Task<string> GetOutputParameter(IAmazonCloudFormation cfClient, string stackName, string outputName)
        {
            var response = await cfClient.DescribeStacksAsync(new DescribeStacksRequest()
            {
                StackName = stackName
            });

            foreach (var output in response.Stacks[0].Outputs)
            {
                if (string.Equals(output.OutputKey, outputName, StringComparison.OrdinalIgnoreCase))
                {
                    return output.OutputValue;
                }
            }

            return null;
        }        
    }
}
