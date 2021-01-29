from nn import NNTraining

training = NNTraining(inputFile='sino-ml-purifyPvE.json', outputFile='sino-ml-purifyPvE.txt')
training.run(numFeatures=20*8, numClasses=4, numHidden1=64, numSteps=10000)
