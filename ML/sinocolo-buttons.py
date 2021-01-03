from nn import NNTraining

training = NNTraining(inputFile='sino-ml-buttons.json', outputFile='sino-ml-buttons.txt')
training.run(numFeatures=16*5, numClasses=4, numSteps=10000)
